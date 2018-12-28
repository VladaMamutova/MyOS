namespace MyOS.ViewModels
{
    public class BitmapRow
    {
        public int RowNumber { get; private set; }
        public string Cell0 { get; private set; }
        public string Cell1 { get; private set; }
        public string Cell2 { get; private set; }
        public string Cell3 { get; private set; }
        public string Cell4 { get; private set; }
        public string Cell5 { get; private set; }
        public string Cell6 { get; private set; }
        public string Cell7 { get; private set; }
        public string Cell8 { get; private set; }
        public string Cell9 { get; private set; }
        public BitmapRow() { }

        public void SetRowNumber(int i) { RowNumber = i; }
        public void Add(int i, string cell)
        {
            switch (i)
            {
                case 0: Cell0 = cell; return;
                case 1: Cell1 = cell; return;
                case 2: Cell2 = cell; return;
                case 3: Cell3 = cell; return;
                case 4: Cell4 = cell; return;
                case 5: Cell5 = cell; return;
                case 6: Cell6 = cell; return;
                case 7: Cell7 = cell; return;
                case 8: Cell8 = cell; return;
                case 9: Cell9 = cell; return;
            }
        }
    }
}
