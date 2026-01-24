using System;
using System.Collections.Generic;
using System.Linq;
using SystemScrap.ServiceLocator.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using static SystemScrap.ServiceLocator.Analysis.AnalysisUtilities;

namespace SystemScrap.ServiceLocator.Analysis
{
    /// <summary>
    /// An Editor Window that displays all currently registered services in the Service Locator.
    /// </summary>
    public class RegisteredServicesWindow : EditorWindow
    {
        public const string EMPTY_DETAILS_MESSAGE = "Select a service to view details.";
        public const string NO_LOCATOR_MESSAGE = "Service locator is not available.";
        public const string NO_SERVICES_MESSAGE = "No services registered for this scope.";
        public const string SELECT_SCENE_MESSAGE = "Select a scene to view services.";
        public const string SELECT_GAME_OBJECT_MESSAGE = "Select a GameObject to view services.";
        public const string SELECT_SCOPE_MESSAGE = "Select a scope to view factories.";
        public const string ALIAS_TYPE_NAME = "SystemScrap.ServiceLocator.Core.ServiceAliasers.AliasTo";

        [SerializeField] private VisualTreeAsset _visualTreeAsset;

        private readonly Dictionary<TabKey, ToolbarToggle> _tabToggles = new();
        private readonly Dictionary<TabKey, VisualElement> _tabContents = new();
        private readonly Dictionary<ListView, string> _detailsEmptyMessages = new();

        private ListView _globalServiceList;
        private ScrollView _globalDetails;
        private ListView _sceneObjectList;
        private ListView _sceneServiceList;
        private ScrollView _sceneDetails;
        private ListView _gameObjectList;
        private ListView _gameObjectServiceList;
        private ScrollView _gameObjectDetails;
        private ListView _managedServiceList;
        private ScrollView _managedDetails;
        private ListView _factoryScopeList;
        private ListView _factoryServiceList;
        private ScrollView _factoryDetails;
        private ToolbarButton _refreshButton;

        private ServiceLocatorSnapshot _snapshot = ServiceLocatorSnapshot.Empty;
        private TabKey _activeTab = TabKey.Global;
        private bool _refreshQueued;
        private bool _initialized;

        /// <summary>
        /// Opens the Registered Services window.
        /// </summary>
        [MenuItem("Window/Registered Services", priority = 100000)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<RegisteredServicesWindow>();
            wnd.titleContent = new GUIContent("Registered Services");
        }

        /// <summary>
        /// Initializes the UI using UXML.
        /// </summary>
        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();

            var visualTree = _visualTreeAsset;

            if (visualTree == null)
            {
                root.Add(new Label($"Missing UXML."));
                return;
            }

            // Setup UI structure
            visualTree.CloneTree(root);
            CacheUi(root);
            
            // Configure components
            ConfigureTabs();
            ConfigureListViews();
            HookEvents();

            _initialized = true;
            RefreshData();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.hierarchyChanged += RequestRefresh;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.hierarchyChanged -= RequestRefresh;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
        }

        private void CacheUi(VisualElement root)
        {
            _refreshButton = root.Q<ToolbarButton>("refresh-button");
            _globalServiceList = root.Q<ListView>("global-service-list");
            _globalDetails = root.Q<ScrollView>("global-details");
            _sceneObjectList = root.Q<ListView>("scene-object-list");
            _sceneServiceList = root.Q<ListView>("scene-service-list");
            _sceneDetails = root.Q<ScrollView>("scene-details");
            _gameObjectList = root.Q<ListView>("gameobject-object-list");
            _gameObjectServiceList = root.Q<ListView>("gameobject-service-list");
            _gameObjectDetails = root.Q<ScrollView>("gameobject-details");
            _managedServiceList = root.Q<ListView>("managed-service-list");
            _managedDetails = root.Q<ScrollView>("managed-details");
            _factoryScopeList = root.Q<ListView>("factory-scope-list");
            _factoryServiceList = root.Q<ListView>("factory-service-list");
            _factoryDetails = root.Q<ScrollView>("factory-details");
        }

        private void ConfigureTabs()
        {
            _tabToggles.Clear();
            _tabContents.Clear();

            // Cache tab controls
            _tabToggles[TabKey.Global] = rootVisualElement.Q<ToolbarToggle>("tab-global");
            _tabToggles[TabKey.Scene] = rootVisualElement.Q<ToolbarToggle>("tab-scene");
            _tabToggles[TabKey.GameObject] = rootVisualElement.Q<ToolbarToggle>("tab-gameobject");
            _tabToggles[TabKey.Managed] = rootVisualElement.Q<ToolbarToggle>("tab-managed");
            _tabToggles[TabKey.Factories] = rootVisualElement.Q<ToolbarToggle>("tab-factories");

            _tabContents[TabKey.Global] = rootVisualElement.Q<VisualElement>("tab-global-content");
            _tabContents[TabKey.Scene] = rootVisualElement.Q<VisualElement>("tab-scene-content");
            _tabContents[TabKey.GameObject] = rootVisualElement.Q<VisualElement>("tab-gameobject-content");
            _tabContents[TabKey.Managed] = rootVisualElement.Q<VisualElement>("tab-managed-content");
            _tabContents[TabKey.Factories] = rootVisualElement.Q<VisualElement>("tab-factories-content");

            // Setup tab switching logic
            foreach (var (key, toggle) in _tabToggles)
            {
                toggle?.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        SetActiveTab(key);
                    }
                });
            }

            SetActiveTab(_activeTab, true);
        }

        private void ConfigureListViews()
        {
            // Set data source labels for all lists
            ConfigureListView<ServiceEntry>(_globalServiceList, entry => entry.DisplayName);
            ConfigureListView<EntryCategory<Scene>>(_sceneObjectList, entry => entry.DisplayName);
            ConfigureListView<ServiceEntry>(_sceneServiceList, entry => entry.DisplayName);
            ConfigureListView<EntryCategory<GameObject>>(_gameObjectList, entry => entry.DisplayName);
            ConfigureListView<ServiceEntry>(_gameObjectServiceList, entry => entry.DisplayName);
            ConfigureListView<ServiceEntry>(_managedServiceList, entry => entry.DisplayName);
            ConfigureListView<EntryCategory<Scope>>(_factoryScopeList, entry => entry.DisplayName);
            ConfigureListView<ServiceEntry>(_factoryServiceList, entry => entry.DisplayName);

            // Hookup selection events
            if (_globalServiceList != null)
                _globalServiceList.selectionChanged += selection =>
                    OnServiceSelectionChanged(selection, _globalDetails, _globalServiceList);

            if (_sceneObjectList != null)
                _sceneObjectList.selectionChanged += OnSceneSelectionChanged;

            if (_sceneServiceList != null)
                _sceneServiceList.selectionChanged += selection =>
                    OnServiceSelectionChanged(selection, _sceneDetails, _sceneServiceList);

            if (_gameObjectList != null)
                _gameObjectList.selectionChanged += OnGameObjectSelectionChanged;

            if (_gameObjectServiceList != null)
                _gameObjectServiceList.selectionChanged += selection =>
                    OnServiceSelectionChanged(selection, _gameObjectDetails, _gameObjectServiceList);

            if (_managedServiceList != null)
                _managedServiceList.selectionChanged += selection =>
                    OnServiceSelectionChanged(selection, _managedDetails, _managedServiceList);

            if (_factoryScopeList != null)
                _factoryScopeList.selectionChanged += OnFactoryScopeSelectionChanged;

            if (_factoryServiceList != null)
                _factoryServiceList.selectionChanged += selection =>
                    OnServiceSelectionChanged(selection, _factoryDetails, _factoryServiceList);
        }

        private void HookEvents()
        {
            if (_refreshButton != null)
                _refreshButton.clicked += RefreshData;
        }

        private void SetActiveTab(TabKey tab, bool force = false)
        {
            if (!force && _activeTab == tab)
                return;

            _activeTab = tab;
            foreach (var pair in _tabContents)
            {
                if (pair.Value == null)
                    continue;

                pair.Value.style.display = pair.Key == tab ? DisplayStyle.Flex : DisplayStyle.None;
            }

            foreach (var (key, toggle) in _tabToggles)
            {
                if (toggle == null)
                    continue;

                var isActive = key == tab;
                toggle.SetValueWithoutNotify(isActive);
                if (isActive)
                    toggle.AddToClassList("tab-toggle--active");
                else
                    toggle.RemoveFromClassList("tab-toggle--active");
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state) => RequestRefresh();

        private void OnSceneOpened(Scene scene, OpenSceneMode mode) => RequestRefresh();

        private void OnSceneClosed(Scene scene) => RequestRefresh();

        private void OnActiveSceneChanged(Scene previous, Scene next) => RequestRefresh();

        private void RequestRefresh()
        {
            if (!_initialized || _refreshQueued)
                return;

            _refreshQueued = true;
            EditorApplication.delayCall += () =>
            {
                _refreshQueued = false;
                if (this == null)
                    return;

                RefreshData();
            };
        }

        private void RefreshData()
        {
            if (!_initialized)
                return;

            _snapshot = ServiceLocatorSnapshot.Capture();
            ApplySnapshot();
        }

        private void ApplySnapshot()
        {
            // Populate Global and Managed services
            if (_globalServiceList != null)
            {
                SetServiceList(_globalServiceList, _snapshot.GlobalServices, _globalDetails,
                    BuildServiceMessage(_snapshot.GlobalServices));
            }

            if (_managedServiceList != null)
            {
                SetServiceList(_managedServiceList, _snapshot.ManagedServices, _managedDetails,
                    BuildServiceMessage(_snapshot.ManagedServices));
            }

            // Populate Scoped services
            if (_sceneObjectList != null)
            {
                SetObjectList(_sceneObjectList, _snapshot.SceneScopes);
                SetServiceList(_sceneServiceList, ServiceLocatorSnapshot.EmptyServices, _sceneDetails,
                    BuildObjectMessage(_snapshot.SceneScopes.Count, SELECT_SCENE_MESSAGE));
            }

            if (_gameObjectList != null)
            {
                SetObjectList(_gameObjectList, _snapshot.GameObjectScopes);
                SetServiceList(_gameObjectServiceList, ServiceLocatorSnapshot.EmptyServices, _gameObjectDetails,
                    BuildObjectMessage(_snapshot.GameObjectScopes.Count, SELECT_GAME_OBJECT_MESSAGE));
            }

            if (_factoryScopeList != null)
            {
                SetObjectList(_factoryScopeList, _snapshot.FactoryScopes);
                SetServiceList(_factoryServiceList, ServiceLocatorSnapshot.EmptyServices, _factoryDetails,
                    BuildObjectMessage(_snapshot.FactoryScopes.Count, SELECT_SCOPE_MESSAGE));
            }
        }

        private void SetObjectList<T>(ListView listView, List<T> items) where T : class
        {
            if (listView == null)
                return;

            listView.itemsSource = items;
            listView.Rebuild();
            listView.ClearSelection();
        }

        private void SetServiceList(ListView listView, List<ServiceEntry> services, ScrollView details,
            string emptyMessage)
        {
            if (listView == null)
                return;

            listView.itemsSource = services;
            listView.Rebuild();
            listView.ClearSelection();
            _detailsEmptyMessages[listView] = emptyMessage;
            SetEmptyDetails(details, emptyMessage);
        }

        private string BuildServiceMessage(ICollection<ServiceEntry> services)
        {
            if (!_snapshot.HasLocator)
                return NO_LOCATOR_MESSAGE;

            return services.Count == 0 ? NO_SERVICES_MESSAGE : EMPTY_DETAILS_MESSAGE;
        }

        private string BuildObjectMessage(int objectCount, string selectMessage)
        {
            if (!_snapshot.HasLocator)
                return NO_LOCATOR_MESSAGE;

            return objectCount == 0 ? NO_SERVICES_MESSAGE : selectMessage;
        }

        private void OnSceneSelectionChanged(IEnumerable<object> selection)
        {
            var entry = selection?.FirstOrDefault() as EntryCategory<Scene>;
            if (entry == null)
            {
                SetServiceList(_sceneServiceList, ServiceLocatorSnapshot.EmptyServices, _sceneDetails,
                    BuildObjectMessage(_snapshot.SceneScopes.Count, SELECT_SCENE_MESSAGE));
                return;
            }

            var message = entry.Services.Count == 0 ? NO_SERVICES_MESSAGE : EMPTY_DETAILS_MESSAGE;
            SetServiceList(_sceneServiceList, entry.Services, _sceneDetails, message);
        }

        private void OnGameObjectSelectionChanged(IEnumerable<object> selection)
        {
            var entry = selection?.FirstOrDefault() as EntryCategory<GameObject>;
            if (entry == null)
            {
                SetServiceList(_gameObjectServiceList, ServiceLocatorSnapshot.EmptyServices, _gameObjectDetails,
                    BuildObjectMessage(_snapshot.GameObjectScopes.Count, SELECT_GAME_OBJECT_MESSAGE));
                return;
            }

            var message = entry.Services.Count == 0 ? NO_SERVICES_MESSAGE : EMPTY_DETAILS_MESSAGE;
            SetServiceList(_gameObjectServiceList, entry.Services, _gameObjectDetails, message);
        }

        private void OnFactoryScopeSelectionChanged(IEnumerable<object> selection)
        {
            var entry = selection?.FirstOrDefault() as EntryCategory<Scope>;
            if (entry == null)
            {
                SetServiceList(_factoryServiceList, ServiceLocatorSnapshot.EmptyServices, _factoryDetails,
                    BuildObjectMessage(_snapshot.FactoryScopes.Count, SELECT_SCOPE_MESSAGE));
                return;
            }

            var message = entry.Services.Count == 0 ? NO_SERVICES_MESSAGE : EMPTY_DETAILS_MESSAGE;
            SetServiceList(_factoryServiceList, entry.Services, _factoryDetails, message);
        }

        private void OnServiceSelectionChanged(IEnumerable<object> selection, ScrollView details, ListView listView)
        {
            var entry = selection?.FirstOrDefault() as ServiceEntry;
            if (entry == null)
            {
                SetEmptyDetails(details, _detailsEmptyMessages.GetValueOrDefault(listView, EMPTY_DETAILS_MESSAGE));
                return;
            }

            ShowServiceDetails(details, entry);
        }

        private void ShowServiceDetails(ScrollView details, ServiceEntry entry)
        {
            if (details == null)
                return;

            details.Clear();

            // Header
            var entryTitle = new Label(entry.DisplayName);
            entryTitle.AddToClassList("details-title");
            details.Add(entryTitle);

            // Basic registration info
            AddDetailRow(details, "Scope", entry.ScopeLabel);

            if (!string.IsNullOrWhiteSpace(entry.OwnerLabel))
                AddDetailRow(details, "Owner", entry.OwnerLabel);

            AddDetailRow(details, "Kind", entry.KindLabel);

            // Type information logic based on registration kind
            if (entry.Kind == ServiceKind.LazyProvider && entry.OriginalType != null)
                AddDetailRow(details, "Original Type", FormatTypeName(entry.OriginalType));
            else if (entry.InstanceType != null)
                AddDetailRow(details, "Instance Type", FormatTypeName(entry.InstanceType));
            else if (entry.OriginalType != null)
                AddDetailRow(details, "Original Type", FormatTypeName(entry.OriginalType));

            if (entry.DescriptorType != null && entry.Kind == ServiceKind.Transient)
                AddDetailRow(details, "Descriptor", entry.DescriptorType.Name);

            // List of all types this service is registered as
            AddRegisteredTypes(details, entry.RegisteredTypes);

            // Reference to the actual object if available
            if (entry.TiedObject != null)
                AddObjectRow(details, entry.TiedObjectLabel, entry.TiedObject);
        }

        private void SetEmptyDetails(ScrollView details, string message)
        {
            if (details == null)
                return;

            details.Clear();
            var label = new Label(message);
            label.AddToClassList("empty-state");
            details.Add(label);
        }

        private void AddDetailRow(VisualElement container, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var row = new VisualElement();
            row.AddToClassList("detail-row");

            var labelElement = new Label(label);
            labelElement.AddToClassList("detail-label");

            var valueElement = new Label(value);
            valueElement.AddToClassList("detail-value");

            row.Add(labelElement);
            row.Add(valueElement);
            container.Add(row);
        }

        private void AddRegisteredTypes(VisualElement container, IReadOnlyList<Type> types)
        {
            if (types == null || types.Count == 0)
                return;

            var header = new Label("Registered Types");
            header.AddToClassList("detail-section-title");
            container.Add(header);

            var list = new VisualElement();
            list.AddToClassList("detail-type-list");
            foreach (var type in types)
            {
                var label = new Label(FormatTypeName(type));
                label.AddToClassList("detail-type-item");
                list.Add(label);
            }

            container.Add(list);
        }

        private void AddObjectRow(VisualElement container, string label, Object obj)
        {
            var row = new VisualElement();
            row.AddToClassList("detail-object-row");

            var objectField = new ObjectField(label)
            {
                value = obj,
                objectType = typeof(Object),
                allowSceneObjects = true
            };
            objectField.AddToClassList("detail-object-field");
            objectField.RegisterValueChangedCallback(_ => objectField.SetValueWithoutNotify(obj));
            objectField.RegisterCallback<MouseUpEvent>(_ => SelectObject(obj));

            row.Add(objectField);
            container.Add(row);
        }

        private void SelectObject(Object obj)
        {
            if (obj == null)
                return;

            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        private void ConfigureListView<T>(ListView listView, Func<T, string> labeler) where T : class
        {
            if (listView == null)
                return;

            listView.selectionType = SelectionType.Single;
            listView.fixedItemHeight = EditorGUIUtility.singleLineHeight + 6f;

            // Define how each item in the list is created
            listView.makeItem = () =>
            {
                var label = new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        paddingLeft = 4f,
                        paddingRight = 4f,
                        whiteSpace = WhiteSpace.NoWrap,
                        overflow = Overflow.Hidden,
                        textOverflow = TextOverflow.Ellipsis
                    }
                };
                return label;
            };

            // Define how data is bound to the created item
            listView.bindItem = (element, index) =>
            {
                if (listView.itemsSource is not { } items || index < 0 || index >= items.Count)
                {
                    ((Label)element).text = string.Empty;
                    return;
                }

                var item = items[index] as T;
                ((Label)element).text = item != null ? labeler(item) : string.Empty;
            };
        }
    }
}