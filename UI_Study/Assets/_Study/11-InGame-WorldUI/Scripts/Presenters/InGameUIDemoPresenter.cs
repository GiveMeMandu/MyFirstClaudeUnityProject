using System;
using R3;
using UIStudy.InGameUI.Models;
using UIStudy.InGameUI.Services;
using UIStudy.InGameUI.Views;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace UIStudy.InGameUI.Presenters
{
    /// <summary>
    /// Presenter for the quarter-view building demo.
    /// Polls mouse clicks via R3 Observable.EveryUpdate, raycasts into 3D scene,
    /// spawns floating resource text, and updates resource totals.
    /// </summary>
    public class InGameUIDemoPresenter : IInitializable, IDisposable
    {
        private readonly InGameUIDemoView _view;
        private readonly FloatingResourceService _floatingService;
        private readonly Camera _camera;
        private readonly BuildingView[] _buildings;
        private readonly CompositeDisposable _disposables = new();

        // Reactive resource totals
        private readonly ReactiveProperty<int> _gold = new(0);
        private readonly ReactiveProperty<int> _wood = new(0);
        private readonly ReactiveProperty<int> _stone = new(0);
        private readonly ReactiveProperty<int> _food = new(0);

        // Pre-defined building configurations
        private static readonly BuildingData[] BuildingConfigs =
        {
            new("Gold Mine",    ResourceType.Gold,  5,  new Color(1.0f, 0.84f, 0f)),    // gold
            new("Lumber Mill",  ResourceType.Wood,  3,  new Color(0.55f, 0.27f, 0.07f)), // saddle brown
            new("Quarry",       ResourceType.Stone, 4,  new Color(0.66f, 0.66f, 0.66f)), // dark gray
            new("Farm",         ResourceType.Food,  6,  new Color(0.13f, 0.55f, 0.13f)), // forest green
            new("Treasury",     ResourceType.Gold,  10, new Color(1.0f, 0.65f, 0f)),     // orange-gold
            new("Bakery",       ResourceType.Food,  8,  new Color(0.86f, 0.44f, 0.58f)), // pink
        };

        public InGameUIDemoPresenter(
            InGameUIDemoView view,
            FloatingResourceService floatingService,
            Camera camera,
            BuildingView[] buildings)
        {
            _view = view;
            _floatingService = floatingService;
            _camera = camera;
            _buildings = buildings;
        }

        public void Initialize()
        {
            // Assign BuildingData to each BuildingView
            SetupBuildings();

            // Bind reactive properties to view
            BindResourceCounters();

            // Poll mouse clicks each frame
            Observable.EveryUpdate()
                .Where(_ => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                .Subscribe(_ => HandleClick())
                .AddTo(_disposables);
        }

        private void SetupBuildings()
        {
            for (int i = 0; i < _buildings.Length; i++)
            {
                if (_buildings[i] == null) continue;

                // Assign config cyclically if more buildings than configs
                var config = BuildingConfigs[i % BuildingConfigs.Length];
                _buildings[i].SetData(config);
            }
        }

        private void BindResourceCounters()
        {
            _gold.Subscribe(v => _view.SetGold(v)).AddTo(_disposables);
            _wood.Subscribe(v => _view.SetWood(v)).AddTo(_disposables);
            _stone.Subscribe(v => _view.SetStone(v)).AddTo(_disposables);
            _food.Subscribe(v => _view.SetFood(v)).AddTo(_disposables);
        }

        private void HandleClick()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            if (!Physics.Raycast(ray, out RaycastHit hit))
                return;

            var building = hit.collider.GetComponent<BuildingView>();
            if (building == null || building.Data == null)
                return;

            var data = building.Data;

            // Spawn floating text
            var floatData = new FloatingResourceData(
                data.Type, data.AmountPerClick, building.transform.position);
            _floatingService.Spawn(floatData, _camera);

            // Update totals
            AddResource(data.Type, data.AmountPerClick);
        }

        private void AddResource(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Gold:
                    _gold.Value += amount;
                    break;
                case ResourceType.Wood:
                    _wood.Value += amount;
                    break;
                case ResourceType.Stone:
                    _stone.Value += amount;
                    break;
                case ResourceType.Food:
                    _food.Value += amount;
                    break;
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _gold.Dispose();
            _wood.Dispose();
            _stone.Dispose();
            _food.Dispose();
        }
    }
}
