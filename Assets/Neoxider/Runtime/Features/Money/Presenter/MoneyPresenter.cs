using System;
using Neo.Runtime.Features.Money.View;
using Neo.Runtime.Features.Money.Model;
using R3;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Features.Money.Presenter
{
    /// <summary>
    /// Presenter for money system
    /// </summary>
    public class MoneyPresenter : IStartable, IDisposable
    {
        [Inject] private readonly MoneyModel _money;
        [Inject] private readonly IMoneyView _view;

        private readonly CompositeDisposable _disp = new();

        public void Start()
        {
            // начальный режим лимита
            _view.SetLimitMode(_money.Max.Value > 0f);

            // подписки
            _money.Balance.AsObservable().Subscribe(OnBalanceChanged).AddTo(_disp);
            _money.Max.AsObservable().Subscribe(OnMaxChanged).AddTo(_disp);
            _money.Percent.AsObservable().Subscribe(OnPercentChanged).AddTo(_disp);

            // кошелёк заполнен — актуально только при лимите
            _money.OnReachedMax.Subscribe(_ => {
                if (_money.Max.Value > 0f)
                    _view.ShowWalletFull(true);
            }).AddTo(_disp);

            // начальный пуш во вью
            PushAll();
        }

        /// <summary>
        /// Push all initial data to view
        /// </summary>
        private void PushAll()
        {
            var hasLimit = _money.Max.Value > 0f;
            _view.SetLimitMode(hasLimit);
            _view.UpdateMaxMoney(_money.Max.Value);
            _view.UpdateMoney(_money.Balance.Value, _money.Max.Value);

            if (hasLimit)
            {
                var p = _money.Percent.Value;
                _view.UpdateMoneyPercentage(p);
                _view.UpdateMoneyPercent100(p * 100f);
            }
        }

        /// <summary>
        /// Handle balance change
        /// </summary>
        /// <param name="balance">New balance</param>
        private void OnBalanceChanged(float balance)
        {
            var hasLimit = _money.Max.Value > 0f;

            _view.UpdateMoney(balance, _money.Max.Value);

            if (hasLimit)
            {
                var p = _money.Percent.Value;
                _view.UpdateMoneyPercentage(p);
                _view.UpdateMoneyPercent100(p * 100f);
            }
        }

        /// <summary>
        /// Handle max amount change
        /// </summary>
        /// <param name="max">New max amount</param>
        private void OnMaxChanged(float max)
        {
            var hasLimit = max > 0f;
            _view.SetLimitMode(hasLimit);
            _view.UpdateMaxMoney(max);

            // при смене лимита пересчёт и пуш процентов только если лимит активен
            if (hasLimit)
            {
                var p = _money.Percent.Value;
                _view.UpdateMoneyPercentage(p);
                _view.UpdateMoneyPercent100(p * 100f);
            }
        }

        /// <summary>
        /// Handle percentage change
        /// </summary>
        /// <param name="p">New percentage</param>
        private void OnPercentChanged(float p)
        {
            if (_money.Max.Value <= 0f) return; // безлимит — не шлём проценты
            _view.UpdateMoneyPercentage(p);
            _view.UpdateMoneyPercent100(p * 100f);
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose() => _disp.Dispose();
    }
}
