using System;
using Neo.Runtime.Features.Health.Model;
using Neo.Runtime.Features.Health.View;
using R3;
using VContainer;

namespace Neo.Runtime.Features.Health.Presenter
{
    /// <summary>
    /// Presenter for health system that handles communication between model and view.
    /// </summary>
    public class HealthPresenter : IDisposable
    {
        private readonly HealthModel _health;
        private readonly IHealthView _view;
        private readonly CompositeDisposable _disp = new();

        /// <summary>
        /// Constructor for health presenter
        /// </summary>
        /// <param name="health">Health model instance</param>
        /// <param name="view">Health view instance</param>
        [Inject]
        public HealthPresenter(HealthModel health, IHealthView view)
        {
            _health = health ?? throw new ArgumentNullException(nameof(health));
            _view = view ?? throw new ArgumentNullException(nameof(view));

            _health.CurrentCurrent.AsObservable().Subscribe(OnHealthChanged).AddTo(_disp);
            _health.Max.AsObservable().Subscribe(OnMaxHealthChanged).AddTo(_disp);
            _health.Percent.AsObservable().Subscribe(OnHealthPercentageChanged).AddTo(_disp);

            _health.OnDead.Subscribe(_ => ShowDead()).AddTo(_disp);
        }

        /// <summary>
        /// Handle health change event
        /// </summary>
        /// <param name="current">Current health value</param>
        private void OnHealthChanged(int current)
        {
            _view.UpdateHealth(current, _health.Max.Value);
        }

        /// <summary>
        /// Handle maximum health change event
        /// </summary>
        /// <param name="max">New maximum health value</param>
        private void OnMaxHealthChanged(int max)
        {
            _view.UpdateMaxHealth(max);
        }

        /// <summary>
        /// Handle health percentage change event
        /// </summary>
        /// <param name="p">Health percentage</param>
        private void OnHealthPercentageChanged(float p)
        {
            _view.UpdateHealthPercentage(p);
            _view.UpdateHealthPercent100(p * 100f);
        }

        /// <summary>
        /// Show death state
        /// </summary>
        private void ShowDead()
        {
            _view.ShowDeath(true);
        }


        /// <summary>
        /// Dispose presenter resources
        /// </summary>
        public void Dispose()
        {
            _disp.Dispose();
        }
    }
}