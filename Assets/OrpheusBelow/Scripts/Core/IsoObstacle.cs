using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Serialized collision classification shared by movement and projectile sweeps.</summary>
    public sealed class IsoObstacle : MonoBehaviour
    {
        public IsoObstacleKind kind = IsoObstacleKind.SolidWall;
    }
}
