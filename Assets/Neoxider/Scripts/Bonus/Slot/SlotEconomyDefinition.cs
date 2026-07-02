using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Neo.Bonus
{
    /// <summary>
    ///     Slot-machine symbol economy: weighted symbol table (drop chance, payouts, optional special
    ///     symbol) plus payline evaluation. Pairs with <c>SpinController</c>/<c>Row</c>, which handle
    ///     the visual spin: pick outcome ids with <see cref="PickWeightedId"/>, feed the settled line
    ///     into <see cref="EvaluateLine"/>, and pay the result into <see cref="Neo.Shop.Money"/> (or
    ///     any wallet) from a UnityEvent or game code.
    /// </summary>
    [NeoDoc("Bonus/Slot/SlotEconomyDefinition.md")]
    [CreateAssetMenu(fileName = "SlotEconomy", menuName = "Neoxider/Bonus/Slot Economy")]
    public sealed class SlotEconomyDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Symbol
        {
            [Tooltip("Display/design name (Empty, Cherry, Jackpot...).")]
            [FormerlySerializedAs("name")]
            public string Name = "";

            [Tooltip("Unique symbol id; the value SpinController rows settle on.")]
            [FormerlySerializedAs("id")]
            public int Id;

            [Tooltip("Primary currency payout when a line of this symbol wins.")]
            [FormerlySerializedAs("moneyReward")]
            public float MoneyReward;

            [Tooltip("Secondary payout (energy, spins, tickets) when a line of this symbol wins.")]
            [FormerlySerializedAs("bonusReward")]
            public int BonusReward;

            [Tooltip("Special symbol: with Force Line On Special, one hit converts the whole payline.")]
            [FormerlySerializedAs("isSpecial")]
            public bool IsSpecial;

            [Tooltip("Relative drop weight; 0 disables the symbol.")] [Min(0f)]
            [FormerlySerializedAs("weight")]
            public float Weight;
        }

        /// <summary>Result of evaluating one settled payline.</summary>
        public readonly struct LineResult
        {
            public LineResult(Symbol symbol, bool isWin, bool specialTriggered)
            {
                Symbol = symbol;
                IsWin = isWin;
                SpecialTriggered = specialTriggered;
            }

            /// <summary>The winning symbol; null when the line lost.</summary>
            public Symbol Symbol { get; }

            public bool IsWin { get; }

            /// <summary>True when the win came from the special-symbol conversion.</summary>
            public bool SpecialTriggered { get; }

            public float MoneyReward => Symbol?.MoneyReward ?? 0f;
            public int BonusReward => Symbol?.BonusReward ?? 0;
        }

        [SerializeField] private Symbol[] _symbols = Array.Empty<Symbol>();

        [Tooltip("When the settled payline contains at least one special symbol, treat the WHOLE line " +
                 "as that symbol (classic wild/jackpot behavior).")]
        [SerializeField]
        private bool _forceLineOnSpecial = true;

        public Symbol[] Symbols => _symbols;
        public bool ForceLineOnSpecial => _forceLineOnSpecial;

        /// <summary>Symbol by id, or null.</summary>
        public Symbol Get(int id)
        {
            for (int i = 0; i < _symbols.Length; i++)
            {
                if (_symbols[i] != null && _symbols[i].Id == id)
                {
                    return _symbols[i];
                }
            }

            return null;
        }

        /// <summary>The first symbol flagged special, or null.</summary>
        public Symbol GetSpecial()
        {
            for (int i = 0; i < _symbols.Length; i++)
            {
                if (_symbols[i] != null && _symbols[i].IsSpecial)
                {
                    return _symbols[i];
                }
            }

            return null;
        }

        /// <summary>Weighted random symbol id; call once per reel to build the target outcome.</summary>
        public int PickWeightedId()
        {
            float total = 0f;
            for (int i = 0; i < _symbols.Length; i++)
            {
                if (_symbols[i] != null && _symbols[i].Weight > 0f)
                {
                    total += _symbols[i].Weight;
                }
            }

            if (total <= 0f)
            {
                return _symbols.Length > 0 && _symbols[0] != null ? _symbols[0].Id : 0;
            }

            float roll = UnityEngine.Random.Range(0f, total);
            float cursor = 0f;
            for (int i = 0; i < _symbols.Length; i++)
            {
                Symbol symbol = _symbols[i];
                if (symbol == null || symbol.Weight <= 0f)
                {
                    continue;
                }

                cursor += symbol.Weight;
                if (roll <= cursor)
                {
                    return symbol.Id;
                }
            }

            for (int i = _symbols.Length - 1; i >= 0; i--)
            {
                if (_symbols[i] != null)
                {
                    return _symbols[i].Id;
                }
            }

            return 0;
        }

        /// <summary>
        ///     Applies the special-symbol rule to a picked outcome line in place: when
        ///     <see cref="ForceLineOnSpecial"/> is on and any id on the line is special, every id
        ///     becomes the special id. Call between picking outcomes and starting the visual spin so
        ///     the reels settle on what will actually be paid.
        /// </summary>
        public void ApplySpecialRule(int[] lineIds)
        {
            if (!_forceLineOnSpecial || lineIds == null)
            {
                return;
            }

            Symbol special = GetSpecial();
            if (special == null)
            {
                return;
            }

            bool hasSpecial = false;
            for (int i = 0; i < lineIds.Length; i++)
            {
                Symbol symbol = Get(lineIds[i]);
                if (symbol != null && symbol.IsSpecial)
                {
                    hasSpecial = true;
                    break;
                }
            }

            if (!hasSpecial)
            {
                return;
            }

            for (int i = 0; i < lineIds.Length; i++)
            {
                lineIds[i] = special.Id;
            }
        }

        /// <summary>
        ///     Evaluates a settled payline: a full line of one symbol wins that symbol's payouts
        ///     (the special rule, when enabled, is applied first). Anything else loses.
        /// </summary>
        public LineResult EvaluateLine(int[] lineIds)
        {
            if (lineIds == null || lineIds.Length == 0)
            {
                return new LineResult(null, false, false);
            }

            bool specialTriggered = false;
            if (_forceLineOnSpecial)
            {
                Symbol special = GetSpecial();
                if (special != null)
                {
                    for (int i = 0; i < lineIds.Length; i++)
                    {
                        Symbol symbol = Get(lineIds[i]);
                        if (symbol != null && symbol.IsSpecial)
                        {
                            specialTriggered = true;
                            return new LineResult(special, true, true);
                        }
                    }
                }
            }

            int firstId = lineIds[0];
            for (int i = 1; i < lineIds.Length; i++)
            {
                if (lineIds[i] != firstId)
                {
                    return new LineResult(null, false, specialTriggered);
                }
            }

            Symbol matched = Get(firstId);
            bool isWin = matched != null && (matched.MoneyReward > 0f || matched.BonusReward > 0);
            return new LineResult(matched, isWin, specialTriggered);
        }
    }
}
