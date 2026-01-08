using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SystemScrap.ServiceLocator.Roslyn
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NullServiceLocatorAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "SL001";

        private const string k_TYPE = "SystemScrap.ServiceLocator.Core.Services";
        private const string k_ATTRIBUTE_FULL = "UnityEngine.RuntimeInitializeOnLoadMethodAttribute";
        private const string k_SET_METHOD = "SetLocator";
        private const int k_ATTRIBUTE_PARAM = 4;

        private const string k_CATEGORY = "Usage";

        private const string k_TITLE = "Invalid Service Locator Access";

        private const string k_MESSAGE = "'{0}' may see the service locator as null in this context";

        private const string k_DESCRIPTION =
            "Unity invokes methods marked with [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] " +
            "before the service locator for 'Services' is guaranteed to be initialized. Any static initialization that accesses 'Services' " +
            "before then may see the locator as null. Move 'Service' access out of static initializers or subsystem-registration methods.";

        private static readonly DiagnosticDescriptor s_Rule = new(DIAGNOSTIC_ID, k_TITLE, k_MESSAGE, k_CATEGORY,
            DiagnosticSeverity.Warning, true, description: k_DESCRIPTION);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_Rule);


        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                                   GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            // check if the node is one we should even be looking at
            if (context.Node is not InvocationExpressionSyntax invocationNode)
                return;
            if (context.ContainingSymbol?.ContainingType is not { } containingType)
                return;
            if (context.SemanticModel.Compilation.GetTypeByMetadataName(k_ATTRIBUTE_FULL) is not
                { } attributeType) return;
            if (context.SemanticModel.GetOperation(invocationNode) is not IInvocationOperation operation)
                return;
            if (Regex.IsMatch(operation.TargetMethod.Name, k_SET_METHOD))
                return;
            var invokedServicesMethod = GetInvokedServicesMethod(operation, context.SemanticModel, context.Compilation);
            if (invokedServicesMethod is null)
                return;

            // we know that the target invocation is Services (directly or via a delegate invoke),
            // now we check the context

            var inStaticInitializer = IsInStaticMemberInitializer(invocationNode);
            var inRuntimeInitMethod = IsInRuntimeInitializeMethod(invocationNode, context.SemanticModel,
                context.SemanticModel.Compilation.GetTypeByMetadataName(k_ATTRIBUTE_FULL));

            if (!inStaticInitializer && !inRuntimeInitMethod)
                return;

            // if we're directly inside the runtime initialize method with the specific param, we can report immediately
            if (inRuntimeInitMethod)
            {
                context.ReportDiagnostic(Diagnostic.Create(s_Rule, invocationNode.GetLocation(),
                    invocationNode.ToString()));
                return;
            }

            // otherwise, we are in a static member initializer; ensure the containing type has such a runtime init method

            var attributes = containingType
                .GetMembers()
                .SelectMany(m => m.GetAttributes())
                .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType))
                .ToArray();
            if (attributes.Length == 0)
                return;
            if (!attributes.Any(a =>
                    a.ConstructorArguments.Any(ca => ca.Value is k_ATTRIBUTE_PARAM)))
                return;

            // we know that the class contains a method marked with the attribute,
            // and that the attribute's argument is the one we disallow
            context.ReportDiagnostic(Diagnostic.Create(s_Rule, invocationNode.GetLocation(),
                invocationNode.ToString()));
        }

        private static IMethodSymbol? GetInvokedServicesMethod(
            IInvocationOperation invocation,
            SemanticModel model,
            Compilation compilation)
        {
            var servicesType = compilation.GetTypeByMetadataName(k_TYPE);
            if (servicesType is null)
                return null;

            // direct invocation where target is in Services
            var directTarget = invocation.TargetMethod.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(servicesType, directTarget.ContainingType))
                return directTarget;

            // delegate invocation
            if (!string.Equals(invocation.TargetMethod.Name, "Invoke", StringComparison.Ordinal))
                return null;

            // try to get method group behind the delegate
            var methodFromInstance = TryResolveMethodGroupFromDelegateInstance(invocation.Instance, model);
            if (methodFromInstance is null)
                return null;

            var resolved = methodFromInstance.OriginalDefinition;
            if (!SymbolEqualityComparer.Default.Equals(servicesType, resolved.ContainingType))
                return null;

            // ignore set method
            if (Regex.IsMatch(resolved.Name, k_SET_METHOD))
                return null;

            return resolved;
        }

        private static IMethodSymbol? TryResolveMethodGroupFromDelegateInstance(
            IOperation? instance,
            SemanticModel model)
        {
            if (instance is null)
                return null;

            instance = Unwrap(instance);

            if (instance is IMethodReferenceOperation directMethodRef)
                return directMethodRef.Method;

            if (instance is ILocalReferenceOperation localRef)
            {
                var local = localRef.Local;
                var declSyntax =
                    local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
                if (declSyntax?.Initializer?.Value is null)
                    return null;

                var initOp = model.GetOperation(declSyntax.Initializer.Value);
                initOp = Unwrap(initOp);

                if (initOp is IMethodReferenceOperation mr)
                    return mr.Method;

                if (initOp is IDelegateCreationOperation dc)
                {
                    var target = Unwrap(dc.Target);
                    if (target is IMethodReferenceOperation mr2)
                        return mr2.Method;
                }
            }

            return null;

            static IOperation? Unwrap(IOperation? op)
            {
                while (op is IConversionOperation conv)
                    op = conv.Operand;

                while (op is IParenthesizedOperation paren)
                    op = paren.Operand;

                return op;
            }
        }

        private static bool IsInStaticMemberInitializer(InvocationExpressionSyntax invocation)
        {
            if (invocation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>()?.Modifiers
                    .Any(SyntaxKind.StaticKeyword) == true)
                return true;

            var declarator = invocation.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            if (declarator?.Initializer?.Value is { } fieldInitValue
                && fieldInitValue.Contains(invocation)) // check if we are within the "= ..." part
            {
                var fieldDecl = declarator.FirstAncestorOrSelf<FieldDeclarationSyntax>();
                if (fieldDecl is not null && fieldDecl.Modifiers.Any(SyntaxKind.StaticKeyword))
                    return true;
            }

            var propDecl = invocation.FirstAncestorOrSelf<PropertyDeclarationSyntax>();

            return propDecl?.Initializer?.Value is { } propInitValue
                   && propInitValue.Contains(invocation)
                   && propDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
        }

        private static bool IsInRuntimeInitializeMethod(SyntaxNode node, SemanticModel model,
            INamedTypeSymbol? attributeType)
        {
            if (attributeType is null)
                return false;

            var methodDecl = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (methodDecl is null)
                return false;

            return model.GetDeclaredSymbol(methodDecl) is { } methodSymbol && methodSymbol.GetAttributes()
                .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)).Any(attr =>
                    attr.ConstructorArguments.Any(ca => ca.Value is k_ATTRIBUTE_PARAM));
        }
    }
}