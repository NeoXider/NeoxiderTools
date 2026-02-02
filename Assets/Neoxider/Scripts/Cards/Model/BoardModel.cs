namespace Neo.Cards
{
    /// <summary>
    ///     Модель стола (board) для общих карт.
    /// </summary>
    public class BoardModel : CardContainerModel
    {
        private readonly int _maxCards;

        public BoardModel(int maxCards = int.MaxValue) : base(CardLocation.Board)
        {
            _maxCards = maxCards;
        }

        public override bool CanAdd(CardData card)
        {
            return Count < _maxCards;
        }
    }
}


