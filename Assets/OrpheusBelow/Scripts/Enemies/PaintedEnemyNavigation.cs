using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Ground pursuit over the painted floor. Committed attacks keep their own heading.</summary>
    public sealed class PaintedEnemyNavigation
    {
        private static readonly List<NavigationGrid> Grids = new List<NavigationGrid>();
        private readonly List<Vector3> _path = new List<Vector3>();
        private NavigationGrid _grid;
        private Vector3 _plannedGoal;
        private Vector3 _legStart;
        private int _cursor;
        private bool _hasPlan;

        public static bool CanReach(PaintedChamberMap map, Vector3 start, Vector3 goal, float clearance = .45f)
        {
            if (map == null || map.definition == null) return false;
            var grid = GetGrid(map.definition, clearance);
            start -= map.transform.position; goal -= map.transform.position;
            if (grid.ClearSegment(start, goal)) return true;
            var path = new List<Vector3>();
            grid.FindPath(start, goal, path);
            return path.Count > 0 && (path[path.Count - 1] - goal).sqrMagnitude < .01f;
        }

        /// <summary>Select only the player's connected floor, within ten metres of actual walking travel.</summary>
        public static bool TryFindSpawn(PaintedChamberMap map, Vector3 anchor, Vector3 desired,
            IList<Vector3> occupied, out Vector3 point)
        {
            point = anchor;
            if (map == null || map.definition == null) return false;
            Vector3 offset = map.transform.position;
            anchor = map.NearestWalkable(anchor, .45f) - offset;
            if (!GetGrid(map.definition, .45f).TryFindSpawn(anchor, desired - offset, occupied, offset, out var local)) return false;
            point = local + offset;
            return true;
        }

        public Vector3 NextWaypoint(PaintedChamberMap map, Vector3 current, Vector3 goal, float clearance = .45f)
        {
            if (map == null || map.definition == null) return current;
            NavigationGrid grid = GetGrid(map.definition, Mathf.Max(0f, clearance));
            Vector3 offset = map.transform.position;
            Vector3 from = current - offset, to = goal - offset;
            from.y = to.y = 0f;
            if (grid.ClearSegment(from, to)) return AtActorHeight(to + offset, current.y);

            bool replan = !_hasPlan || _grid != grid || (to - _plannedGoal).sqrMagnitude > 1f;
            if (!replan)
            {
                if (_path.Count == 0) replan = (from - _legStart).sqrMagnitude > 1f;
                else if (_cursor >= _path.Count)
                    replan = (to - _plannedGoal).sqrMagnitude > .0025f || (from - _legStart).sqrMagnitude > 1f;
                else replan = PointSegmentDistanceSquared(XZ(from), XZ(_legStart), XZ(_path[_cursor])) > 1f;
            }
            if (replan)
            {
                _grid = grid;
                _plannedGoal = to;
                _legStart = from;
                _cursor = 0;
                _hasPlan = true;
                _path.Clear();
                grid.FindPath(from, to, _path);
            }

            while (_cursor < _path.Count && (from - _path[_cursor]).sqrMagnitude < .0025f)
            {
                _legStart = _path[_cursor];
                _cursor++;
            }
            if (_cursor >= _path.Count) return current;

            // String-pull only through swept-clear segments; clear endpoints alone can cut lava corners.
            while (_cursor + 1 < _path.Count && grid.ClearSegment(from, _path[_cursor + 1]))
            {
                _legStart = from;
                _cursor++;
            }
            if (!grid.ClearSegment(from, _path[_cursor]))
            {
                // Physics or another actor can displace us even before the one-unit stray threshold.
                _hasPlan = false;
                return current;
            }
            return AtActorHeight(_path[_cursor] + offset, current.y);
        }

        /// <summary>Discard geometry when an authored layout is edited at runtime.</summary>
        public static void Invalidate(PaintedChamberDefinition layout = null)
        {
            if (layout == null) Grids.Clear();
            else Grids.RemoveAll(grid => grid.Layout == layout);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => Grids.Clear();

        private static NavigationGrid GetGrid(PaintedChamberDefinition layout, float clearance)
        {
            for (int i = Grids.Count - 1; i >= 0; i--)
            {
                if (Grids[i].Layout == null) { Grids.RemoveAt(i); continue; }
                if (Grids[i].Layout == layout && Mathf.Approximately(Grids[i].Clearance, clearance)) return Grids[i];
            }
            var grid = new NavigationGrid(layout, clearance);
            Grids.Add(grid);
            return grid;
        }

        private static Vector3 AtActorHeight(Vector3 point, float height) { point.y = height; return point; }
        private static Vector2 XZ(Vector3 point) => new Vector2(point.x, point.z);

        private static float PointSegmentDistanceSquared(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, delta) / Mathf.Max(.000001f, delta.sqrMagnitude));
            return (point - a - delta * t).sqrMagnitude;
        }

        private static float SegmentDistanceSquared(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            Vector2 ab = b - a, cd = d - c, ac = c - a;
            float cross = ab.x * cd.y - ab.y * cd.x;
            if (Mathf.Abs(cross) > .000001f)
            {
                float t = (ac.x * cd.y - ac.y * cd.x) / cross;
                float u = (ac.x * ab.y - ac.y * ab.x) / cross;
                if (t >= 0f && t <= 1f && u >= 0f && u <= 1f) return 0f;
            }
            return Mathf.Min(Mathf.Min(PointSegmentDistanceSquared(a, c, d), PointSegmentDistanceSquared(b, c, d)),
                Mathf.Min(PointSegmentDistanceSquared(c, a, b), PointSegmentDistanceSquared(d, a, b)));
        }

        private sealed class NavigationGrid
        {
            private const int Width = 96, Height = 64;
            private static readonly int[] DeltaX = { 1, -1, 0, 0 };
            private static readonly int[] DeltaY = { 0, 0, 1, -1 };
            public readonly PaintedChamberDefinition Layout;
            public readonly float Clearance;
            private readonly List<Vector2> _edgeStarts = new List<Vector2>();
            private readonly List<Vector2> _edgeEnds = new List<Vector2>();
            private readonly Vector2 _wellMin;
            private readonly Vector2 _wellMax;
            private Vector3[] _nodes;
            private bool[] _walkable;
            private byte[] _connections;
            private int[] _parents;
            private int[] _queue;

            public bool TryFindSpawn(Vector3 anchor, Vector3 desired, IList<Vector3> occupied, Vector3 offset, out Vector3 point)
            {
                point = anchor;
                EnsureNodes();
                int first = NearestNode(anchor, true);
                if (first < 0) return false;
                var distances = new float[_nodes.Length];
                for (int i = 0; i < distances.Length; i++) distances[i] = float.PositiveInfinity;
                distances[first] = Vector3.Distance(anchor, _nodes[first]);
                int head = 0, tail = 0, best = -1;
                float score = float.PositiveInfinity;
                _queue[tail++] = first;
                while (head < tail)
                {
                    int at = _queue[head++];
                    Vector3 candidate = _nodes[at];
                    bool separated = true;
                    if (occupied != null) foreach (var other in occupied)
                        if ((candidate + offset - other).sqrMagnitude < 1.6f * 1.6f) { separated = false; break; }
                    if (separated && (candidate - anchor).sqrMagnitude >= 9f && Layout.IsWalkable(candidate, .65f))
                    {
                        float candidateScore = (candidate - desired).sqrMagnitude + Mathf.Abs(distances[at] - 6f) * .15f;
                        if (candidateScore < score) { best = at; score = candidateScore; }
                    }
                    byte connections = Connections(at);
                    for (int direction = 0; direction < 4; direction++)
                    {
                        if ((connections & (1 << direction)) == 0) continue;
                        int next = at + DeltaX[direction] + DeltaY[direction] * Width;
                        if (!float.IsPositiveInfinity(distances[next])) continue;
                        float distance = distances[at] + Vector3.Distance(candidate, _nodes[next]);
                        if (distance > 10f) continue;
                        distances[next] = distance; _queue[tail++] = next;
                    }
                }
                if (best < 0) return false;
                point = _nodes[best]; return true;
            }

            public NavigationGrid(PaintedChamberDefinition layout, float clearance)
            {
                Layout = layout;
                Clearance = clearance;
                AddEdges(layout.walkableBoundary);
                if (layout.cover != null)
                    foreach (var cover in layout.cover) AddEdges(cover.footprint);
                // RuntimeData places a world-aligned 1.2 x 1.2 solid basin outside the painted cover list.
                Vector3 well = layout.ImageToGround(layout.well);
                if (layout.walkableBoundary != null && layout.walkableBoundary.Length >= 3)
                    well = layout.NearestWalkable(well, .8f);
                _wellMin = XZ(well) - Vector2.one * .6f;
                _wellMax = XZ(well) + Vector2.one * .6f;
                var corners = new[] { _wellMin, new Vector2(_wellMax.x, _wellMin.y), _wellMax, new Vector2(_wellMin.x, _wellMax.y) };
                for (int i = 0; i < corners.Length; i++)
                {
                    _edgeStarts.Add(corners[i]);
                    _edgeEnds.Add(corners[(i + 1) % corners.Length]);
                }
            }

            private void AddEdges(Vector2[] polygon)
            {
                if (polygon == null) return;
                for (int i = 0; i < polygon.Length; i++)
                {
                    _edgeStarts.Add(XZ(Layout.ImageToGround(polygon[i])));
                    _edgeEnds.Add(XZ(Layout.ImageToGround(polygon[(i + 1) % polygon.Length])));
                }
            }

            public bool ClearSegment(Vector3 start, Vector3 end)
            {
                if (!IsWalkable(start) || !IsWalkable(end)) return false;
                Vector2 a = XZ(start), b = XZ(end);
                float radiusSquared = Clearance * Clearance;
                for (int i = 0; i < _edgeStarts.Count; i++)
                    if (SegmentDistanceSquared(a, b, _edgeStarts[i], _edgeEnds[i]) < radiusSquared + .000001f) return false;
                return true;
            }

            private bool IsWalkable(Vector3 point)
            {
                if (Layout.walkableBoundary == null || Layout.walkableBoundary.Length < 3 || !Layout.IsWalkable(point, Clearance)) return false;
                Vector2 ground = XZ(point);
                var nearest = new Vector2(Mathf.Clamp(ground.x, _wellMin.x, _wellMax.x), Mathf.Clamp(ground.y, _wellMin.y, _wellMax.y));
                return (ground - nearest).sqrMagnitude > Clearance * Clearance + .000001f;
            }

            private void EnsureNodes()
            {
                if (_nodes != null) return;
                _nodes = new Vector3[Width * Height];
                _walkable = new bool[_nodes.Length];
                _connections = new byte[_nodes.Length];
                _parents = new int[_nodes.Length];
                _queue = new int[_nodes.Length];
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        int index = y * Width + x;
                        _nodes[index] = Layout.ImageToGround(new Vector2(x * 16 + 8, y * 16 + 8));
                        _walkable[index] = IsWalkable(_nodes[index]);
                        _connections[index] = byte.MaxValue;
                    }
            }

            private int NearestNode(Vector3 point, bool requireConnection)
            {
                int best = -1;
                float bestDistance = float.PositiveInfinity;
                for (int i = 0; i < _nodes.Length; i++)
                {
                    if (!_walkable[i]) continue;
                    float distance = (point - _nodes[i]).sqrMagnitude;
                    if (distance >= bestDistance || (requireConnection && !ClearSegment(point, _nodes[i]))) continue;
                    best = i;
                    bestDistance = distance;
                }
                return best;
            }

            private byte Connections(int index)
            {
                if (_connections[index] != byte.MaxValue) return _connections[index];
                byte mask = 0;
                for (int direction = 0; direction < 4; direction++)
                {
                    int x = index % Width + DeltaX[direction], y = index / Width + DeltaY[direction];
                    if (x < 0 || y < 0 || x >= Width || y >= Height) continue;
                    int next = y * Width + x;
                    if (_walkable[next] && ClearSegment(_nodes[index], _nodes[next])) mask |= (byte)(1 << direction);
                }
                _connections[index] = mask;
                return mask;
            }

            public void FindPath(Vector3 start, Vector3 goal, List<Vector3> result)
            {
                EnsureNodes();
                int first = NearestNode(start, true);
                bool exactGoal = IsWalkable(goal);
                int last = NearestNode(goal, exactGoal);
                if (first < 0 || last < 0) return;
                for (int i = 0; i < _parents.Length; i++) _parents[i] = -1;
                int head = 0, tail = 0;
                _queue[tail++] = first;
                _parents[first] = first;
                while (head < tail && _parents[last] < 0)
                {
                    int at = _queue[head++];
                    byte connections = Connections(at);
                    for (int direction = 0; direction < 4; direction++)
                    {
                        if ((connections & (1 << direction)) == 0) continue;
                        int next = at + DeltaX[direction] + DeltaY[direction] * Width;
                        if (_parents[next] >= 0) continue;
                        _parents[next] = at;
                        _queue[tail++] = next;
                    }
                }
                if (_parents[last] < 0) return;
                for (int at = last; ; at = _parents[at])
                {
                    result.Add(_nodes[at]);
                    if (at == first) break;
                }
                result.Reverse();
                if (exactGoal) result.Add(goal);
            }
        }
    }
}
