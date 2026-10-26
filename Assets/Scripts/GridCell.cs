using UnityEngine;

namespace ArcaneWorkshop
{
    public sealed class GridCell : MonoBehaviour
    {
        public int row { get; private set; }
        public int column { get; private set; }
        public ArcaneBuilding foundation { get; set; }
        public ArcaneBuilding building { get; set; }

        public void Configure(int newRow, int newColumn)
        {
            row = newRow;
            column = newColumn;
            name = "Grid Cell " + row + "," + column;
        }
    }
}
