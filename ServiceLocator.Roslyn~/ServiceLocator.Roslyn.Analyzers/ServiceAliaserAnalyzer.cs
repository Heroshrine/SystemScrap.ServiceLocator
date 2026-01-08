using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SystemScrap.ServiceLocator.Roslyn
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ServiceAliaserAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "SL002";
        public const string DIAGNOSTIC_ID_SAME = "SL003";

        private const string k_CATEGORY = "Usage";

        private const string k_TITLE = "Invalid Service Alias";
        private const string k_TITLE_SAME = "Service Alias is the same type";

        private const string k_MESSAGE = "Service alias '{0}' is not assignable to '{1}'";
        private const string k_MESSAGE_SAME = "Service alias '{0}' is the same type as '{1}'";

        private const string k_DESCRIPTION = "Service aliases must be assignable to the service type they alias.";
        private const string k_DESCRIPTION_SAME = "Service aliases must be different types.";

        private const string k_TYPE_FULL = "SystemScrap.ServiceLocator.Framework.IServiceAliaser`1";
        private const string k_METHOD = "As";

        private static readonly DiagnosticDescriptor s_Rule = new(DIAGNOSTIC_ID, k_TITLE, k_MESSAGE, k_CATEGORY,
            DiagnosticSeverity.Error, true, description: k_DESCRIPTION);

        private static readonly DiagnosticDescriptor s_RuleSame = new(DIAGNOSTIC_ID_SAME, k_TITLE_SAME, k_MESSAGE_SAME,
            k_CATEGORY, DiagnosticSeverity.Error, true, description: k_DESCRIPTION_SAME);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(s_Rule, s_RuleSame);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                                   GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not InvocationExpressionSyntax invocationNode)
                return;
            if (context.Compilation.GetTypeByMetadataName(k_TYPE_FULL) is not { } aliaser)
                return;
            if (aliaser.GetMembers(k_METHOD).FirstOrDefault() is not IMethodSymbol asMethod)
                return;
            if (context.SemanticModel.GetOperation(invocationNode) is not IInvocationOperation invocation)
                return;
            if (!SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.OriginalDefinition, asMethod)
                && (invocation.TargetMethod.ContainingType.FindImplementationForInterfaceMember(asMethod) is not
                        IMethodSymbol implementation
                    || !SymbolEqualityComparer.Default.Equals(implementation.OriginalDefinition, asMethod)))
                return;

            // we are at the correct method call

            var aliasType = invocation.TargetMethod.TypeArguments.Single();
            var serviceType = (invocation.Instance?.Type as INamedTypeSymbol)?.TypeArguments.Single();

            if (serviceType is null) return;
            var conversion = context.Compilation.ClassifyConversion(serviceType, aliasType);
            if (IsRuntimeAssignable(conversion)) return;

            if (conversion.IsIdentity)
                context.ReportDiagnostic(Diagnostic.Create(s_RuleSame, invocationNode.GetLocation(),
                    serviceType.ToDisplayString(), aliasType.ToDisplayString()));
            else
                context.ReportDiagnostic(Diagnostic.Create(s_Rule, invocationNode.GetLocation(),
                    serviceType.ToDisplayString(), aliasType.ToDisplayString()));
        }

        private static bool IsRuntimeAssignable(Conversion conversion)
        {
            if (!conversion.Exists) return false;

            // extremely similar to IsAssignableFrom, but with IsIdentity removed so we can report that

            if (conversion.IsNullLiteral) return true;
            if (conversion.IsIdentity) return false;
            if (conversion is { IsReference: true, IsImplicit: true }) return true;
            if (conversion is { IsBoxing: true, IsImplicit: true }) return true;
            if (conversion is { IsNullable: true, IsImplicit: true }) return true;

            return false;
        }
    }
}