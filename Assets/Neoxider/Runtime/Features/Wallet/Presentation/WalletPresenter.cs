using System;
using System.Collections.Generic;
using Neo.Runtime.Features.Money.Model;
using Neo.Runtime.Features.Wallet.Data;
using Neo.Runtime.Features.Wallet.Domain;
using Neo.Runtime.Features.Wallet.Presentation;
using R3;
using Serilog;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Features.Wallet.Presenter
{
    /// <summary>
    /// One presenter for the entire wallet: subscribes each MoneyViewWithId to its own MoneyModel.
    /// </summary>
    public class WalletPresenter : IStartable, IDisposable
    {
        [Inject] private readonly WalletModel _wallet;
        [Inject] private readonly WalletConfig _config;
        [Inject] private readonly IEnumerable<MoneyViewWithId> _views;
        [Inject] private readonly ILogger _logger;

        private readonly CompositeDisposable _disp = new();

        /// <summary>
        /// Starts the presenter and binds all views to their respective models.
        /// </summary>
        public void Start()
        {
            foreach (MoneyViewWithId view in _views)
            {
                if (!_config.TryGet(view.CurrencyId, out _))
                {
                    _logger.Warning("[WalletPresenter.Start] Unknown CurrencyId: {CurrencyId}", view.CurrencyId);
                    continue;
                }

                MoneyModel model = _wallet.Get(view.CurrencyId);
                
                _logger.Information("[WalletPresenter.Start] Binding view for currency: {CurrencyId}, Balance: {Balance}, Max: {Max}", 
                    view.CurrencyId, model.Balance.Value, model.Max.Value);
                
                bool hasLimit = model.Max.Value > 0f;
                view.SetLimitMode(hasLimit);
                view.UpdateMaxMoney(model.Max.Value);
                view.UpdateMoney(model.Balance.Value, model.Max.Value);
                if (hasLimit)
                {
                    float p = model.Percent.Value;
                    view.UpdateMoneyPercentage(p);
                    view.UpdateMoneyPercent100(p * 100f);
                }
                
                model.Balance.AsObservable().Subscribe(b =>
                {
                    _logger.Information("[WalletPresenter.OnBalanceChanged] Currency: {CurrencyId}, Balance: {Balance}/{Max}", 
                        view.CurrencyId, b, model.Max.Value);
                    
                    view.UpdateMoney(b, model.Max.Value);
                    if (model.Max.Value > 0f)
                    {
                        float p = model.Percent.Value;
                        view.UpdateMoneyPercentage(p);
                        view.UpdateMoneyPercent100(p * 100f);
                    }
                }).AddTo(_disp);

                model.Max.AsObservable().Subscribe(m =>
                {
                    bool hl = m > 0f;
                    view.SetLimitMode(hl);
                    view.UpdateMaxMoney(m);
                    if (hl)
                    {
                        float p = model.Percent.Value;
                        view.UpdateMoneyPercentage(p);
                        view.UpdateMoneyPercent100(p * 100f);
                    }
                }).AddTo(_disp);

                model.OnReachedMax.Subscribe(_ =>
                {
                    if (model.Max.Value > 0f)
                    {
                        _logger.Warning("[WalletPresenter.OnReachedMax] Wallet full for currency: {CurrencyId}", view.CurrencyId);
                        view.ShowWalletFull(true);
                    }
                }).AddTo(_disp);
            }
        }

        /// <summary>
        /// Disposes the presenter and cleans up subscriptions.
        /// </summary>
        public void Dispose()
        {
            _disp.Dispose();
        }
    }
}
