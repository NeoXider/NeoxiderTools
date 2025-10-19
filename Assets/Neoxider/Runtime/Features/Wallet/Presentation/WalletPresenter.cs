using System;
using System.Collections.Generic;
using Neo.Runtime.Features.Money.Model;
using Neo.Runtime.Features.Wallet.Data;
using Neo.Runtime.Features.Wallet.Domain;
using Neo.Runtime.Features.Wallet.Presentation;
using R3;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Features.Wallet.Presenter
{
    /// <summary>
    /// Один презентер на весь кошелёк: подписывает каждую MoneyViewWithId на свою MoneyModel.
    /// </summary>
    public class WalletPresenter : IStartable, IDisposable
    {
        private readonly WalletModel _wallet;
        private readonly WalletConfig _config;
        private readonly IEnumerable<MoneyViewWithId> _views;

        private readonly CompositeDisposable _disp = new();

        [Inject]
        public WalletPresenter(WalletModel wallet, WalletConfig config, IEnumerable<MoneyViewWithId> views)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _views = views ?? throw new ArgumentNullException(nameof(views));
        }

        public void Start()
        {
            foreach (MoneyViewWithId view in _views)
            {
                if (!_config.TryGet(view.CurrencyId, out _))
                {
                    UnityEngine.Debug.LogWarning($"WalletPresenter: Unknown CurrencyId on view: {view.CurrencyId}");
                    continue;
                }

                MoneyModel model = _wallet.Get(view.CurrencyId);
                
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
                        view.ShowWalletFull(true);
                    }
                }).AddTo(_disp);
            }
        }

        public void Dispose()
        {
            _disp.Dispose();
        }
    }
}