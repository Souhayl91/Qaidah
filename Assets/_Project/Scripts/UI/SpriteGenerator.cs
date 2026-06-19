using UnityEngine;
using System.Collections.Generic;

namespace SimpleQaidah.UI
{
    /// <summary>
    /// Generates and caches procedural sprites at runtime.
    /// All sprites are white — tint via Image.color.
    /// </summary>
    public static class SpriteGenerator
    {
        private static readonly Dictionary<string, Sprite> _cache = new();

        // ─── Rounded Rectangle ──────────────────────────────────

        public static Sprite RoundedRect(int width, int height, int radius)
        {
            string key = $"rrect_{width}_{height}_{radius}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[width * height];
            var white = new Color32(255, 255, 255, 255);
            var clear = new Color32(0, 0, 0, 0);

            int r = Mathf.Min(radius, Mathf.Min(width, height) / 2);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = true;

                    // Check corners
                    if (x < r && y < r)
                        inside = CornerCheck(x, y, r, r, r);
                    else if (x >= width - r && y < r)
                        inside = CornerCheck(x, y, width - r - 1, r, r);
                    else if (x < r && y >= height - r)
                        inside = CornerCheck(x, y, r, height - r - 1, r);
                    else if (x >= width - r && y >= height - r)
                        inside = CornerCheck(x, y, width - r - 1, height - r - 1, r);

                    pixels[y * width + x] = inside ? white : clear;
                }
            }

            // Anti-alias the corners
            AntiAliasCorners(pixels, width, height, r);

            tex.SetPixels32(pixels);
            tex.Apply();

            // Create sliced sprite with proper borders for 9-slice scaling
            int border = r;
            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border) // left, bottom, right, top
            );
            sprite.name = key;

            _cache[key] = sprite;
            return sprite;
        }

        private static bool CornerCheck(int x, int y, int cx, int cy, int r)
        {
            float dx = x - cx;
            float dy = y - cy;
            return dx * dx + dy * dy <= r * r;
        }

        private static void AntiAliasCorners(Color32[] pixels, int w, int h, int r)
        {
            if (r <= 1) return;

            // Process each corner region
            ProcessCornerAA(pixels, w, h, r, r, r);                    // bottom-left
            ProcessCornerAA(pixels, w, h, w - r - 1, r, r);           // bottom-right
            ProcessCornerAA(pixels, w, h, r, h - r - 1, r);           // top-left
            ProcessCornerAA(pixels, w, h, w - r - 1, h - r - 1, r);   // top-right
        }

        private static void ProcessCornerAA(Color32[] pixels, int w, int h, int cx, int cy, int r)
        {
            int startX = Mathf.Max(0, cx - r);
            int endX = Mathf.Min(w - 1, cx + r);
            int startY = Mathf.Max(0, cy - r);
            int endY = Mathf.Min(h - 1, cy + r);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = r - 0.5f;

                    if (dist > edge - 1f && dist < edge + 1f)
                    {
                        float alpha = Mathf.Clamp01(edge + 0.5f - dist);
                        byte a = (byte)(alpha * 255);
                        pixels[y * w + x] = new Color32(255, 255, 255, a);
                    }
                }
            }
        }

        // ─── Circle ─────────────────────────────────────────────

        public static Sprite Circle(int diameter)
        {
            string key = $"circle_{diameter}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[diameter * diameter];
            float center = (diameter - 1) / 2f;
            float radius = diameter / 2f;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < radius - 0.5f)
                    {
                        pixels[y * diameter + x] = new Color32(255, 255, 255, 255);
                    }
                    else if (dist < radius + 0.5f)
                    {
                        float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                        pixels[y * diameter + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                    }
                    else
                    {
                        pixels[y * diameter + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, diameter, diameter),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;

            _cache[key] = sprite;
            return sprite;
        }

        // ─── Star ───────────────────────────────────────────────

        public static Sprite Star(int size, int points = 5, float innerRatio = 0.4f)
        {
            string key = $"star_{size}_{points}_{innerRatio:F2}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float center = (size - 1) / 2f;
            float outerR = size / 2f - 2f; // small margin
            float innerR = outerR * innerRatio;

            // Generate star polygon vertices
            int vertCount = points * 2;
            var verts = new Vector2[vertCount];
            float angleStep = Mathf.PI * 2f / vertCount;
            float startAngle = -Mathf.PI / 2f; // top point

            for (int i = 0; i < vertCount; i++)
            {
                float angle = startAngle + i * angleStep;
                float r = (i % 2 == 0) ? outerR : innerR;
                verts[i] = new Vector2(
                    center + Mathf.Cos(angle) * r,
                    center + Mathf.Sin(angle) * r
                );
            }

            // Fill using point-in-polygon test
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (PointInPolygon(x, y, verts))
                    {
                        pixels[y * size + x] = new Color32(255, 255, 255, 255);
                    }
                    else
                    {
                        // Anti-alias edges
                        float minDist = MinDistToPolygonEdge(x, y, verts);
                        if (minDist < 1.5f)
                        {
                            float alpha = Mathf.Clamp01(1.5f - minDist);
                            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                        }
                        else
                        {
                            pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;

            _cache[key] = sprite;
            return sprite;
        }

        private static bool PointInPolygon(float px, float py, Vector2[] verts)
        {
            bool inside = false;
            int j = verts.Length - 1;
            for (int i = 0; i < verts.Length; i++)
            {
                if ((verts[i].y > py) != (verts[j].y > py) &&
                    px < (verts[j].x - verts[i].x) * (py - verts[i].y) / (verts[j].y - verts[i].y) + verts[i].x)
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }

        private static float MinDistToPolygonEdge(float px, float py, Vector2[] verts)
        {
            float minDist = float.MaxValue;
            int j = verts.Length - 1;
            for (int i = 0; i < verts.Length; i++)
            {
                float dist = PointToSegmentDist(px, py, verts[j].x, verts[j].y, verts[i].x, verts[i].y);
                if (dist < minDist) minDist = dist;
                j = i;
            }
            return minDist;
        }

        private static float PointToSegmentDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float lenSq = dx * dx + dy * dy;
            if (lenSq < 0.0001f) return Mathf.Sqrt((px - ax) * (px - ax) + (py - ay) * (py - ay));

            float t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / lenSq);
            float projX = ax + t * dx;
            float projY = ay + t * dy;
            float distX = px - projX;
            float distY = py - projY;
            return Mathf.Sqrt(distX * distX + distY * distY);
        }

        // ─── Checkmark Icon ────────────────────────────────────

        public static Sprite Checkmark(int size)
        {
            string key = $"check_{size}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];

            // Checkmark: two thick lines
            // Short leg: from (20%, 50%) to (40%, 30%)
            // Long leg: from (40%, 30%) to (80%, 75%)
            float thickness = size * 0.12f;

            Vector2 p1 = new Vector2(size * 0.20f, size * 0.50f);
            Vector2 p2 = new Vector2(size * 0.40f, size * 0.30f);
            Vector2 p3 = new Vector2(size * 0.80f, size * 0.75f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d1 = PointToSegmentDist(x, y, p1.x, p1.y, p2.x, p2.y);
                    float d2 = PointToSegmentDist(x, y, p2.x, p2.y, p3.x, p3.y);
                    float d = Mathf.Min(d1, d2);

                    if (d < thickness)
                    {
                        pixels[y * size + x] = new Color32(255, 255, 255, 255);
                    }
                    else if (d < thickness + 1f)
                    {
                        float alpha = Mathf.Clamp01(thickness + 1f - d);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;

            _cache[key] = sprite;
            return sprite;
        }

        // ─── Lock Icon ──────────────────────────────────────────

        public static Sprite LockIcon(int size)
        {
            string key = $"lock_{size}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float cx = size / 2f;

            // Body: rounded rectangle in lower 55%
            float bodyTop = size * 0.55f;
            float bodyBottom = size * 0.08f;
            float bodyLeft = size * 0.22f;
            float bodyRight = size * 0.78f;
            float bodyRadius = size * 0.08f;

            // Shackle: arc in upper portion
            float shackleCy = bodyTop;
            float shackleOuterR = size * 0.22f;
            float shackleInnerR = size * 0.13f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool filled = false;

                    // Body (rounded rect)
                    if (x >= bodyLeft && x <= bodyRight && y >= bodyBottom && y <= bodyTop)
                    {
                        filled = true;
                        // Round corners
                        float bw = bodyRight - bodyLeft;
                        float bh = bodyTop - bodyBottom;
                        float lx = x - bodyLeft;
                        float ly = y - bodyBottom;

                        if (lx < bodyRadius && ly < bodyRadius)
                            filled = CornerCheck((int)lx, (int)ly, (int)bodyRadius, (int)bodyRadius, (int)bodyRadius);
                        else if (lx > bw - bodyRadius && ly < bodyRadius)
                            filled = CornerCheck((int)lx, (int)ly, (int)(bw - bodyRadius), (int)bodyRadius, (int)bodyRadius);
                        else if (lx < bodyRadius && ly > bh - bodyRadius)
                            filled = CornerCheck((int)lx, (int)ly, (int)bodyRadius, (int)(bh - bodyRadius), (int)bodyRadius);
                        else if (lx > bw - bodyRadius && ly > bh - bodyRadius)
                            filled = CornerCheck((int)lx, (int)ly, (int)(bw - bodyRadius), (int)(bh - bodyRadius), (int)bodyRadius);
                    }

                    // Shackle (semi-circle arc, top half only)
                    if (y >= bodyTop)
                    {
                        float dx = x - cx;
                        float dy = y - shackleCy;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist <= shackleOuterR && dist >= shackleInnerR && dy >= 0)
                        {
                            filled = true;
                        }
                    }

                    pixels[y * size + x] = filled
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;

            _cache[key] = sprite;
            return sprite;
        }

        // ─── Speaker Icon ───────────────────────────────────────

        public static Sprite SpeakerIcon(int size)
        {
            string key = $"speaker_{size}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float cx = size * 0.35f;
            float cy = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool filled = false;

                    // Speaker body: rectangle on left
                    float bodyLeft = size * 0.15f;
                    float bodyRight = size * 0.30f;
                    float bodyTop = cy + size * 0.10f;
                    float bodyBottom = cy - size * 0.10f;

                    if (x >= bodyLeft && x <= bodyRight && y >= bodyBottom && y <= bodyTop)
                        filled = true;

                    // Speaker cone: triangle from bodyRight to right
                    float coneRight = size * 0.48f;
                    float coneHalfH = size * 0.25f;
                    if (x > bodyRight && x <= coneRight)
                    {
                        float t = (x - bodyRight) / (coneRight - bodyRight);
                        float halfH = Mathf.Lerp(size * 0.10f, coneHalfH, t);
                        if (y >= cy - halfH && y <= cy + halfH)
                            filled = true;
                    }

                    // Sound waves (arcs)
                    if (!filled)
                    {
                        float waveCx = size * 0.45f;
                        float dx = x - waveCx;
                        float dy = y - cy;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float thickness = size * 0.035f;

                        // Only draw arcs on the right side (x > waveCx)
                        if (x > waveCx)
                        {
                            // Wave 1
                            float r1 = size * 0.15f;
                            if (Mathf.Abs(dist - r1) < thickness)
                            {
                                float angle = Mathf.Atan2(dy, dx);
                                if (angle > -Mathf.PI / 3f && angle < Mathf.PI / 3f)
                                    filled = true;
                            }

                            // Wave 2
                            float r2 = size * 0.25f;
                            if (Mathf.Abs(dist - r2) < thickness)
                            {
                                float angle = Mathf.Atan2(dy, dx);
                                if (angle > -Mathf.PI / 3.5f && angle < Mathf.PI / 3.5f)
                                    filled = true;
                            }

                            // Wave 3
                            float r3 = size * 0.35f;
                            if (Mathf.Abs(dist - r3) < thickness)
                            {
                                float angle = Mathf.Atan2(dy, dx);
                                if (angle > -Mathf.PI / 4f && angle < Mathf.PI / 4f)
                                    filled = true;
                            }
                        }
                    }

                    pixels[y * size + x] = filled
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;

            _cache[key] = sprite;
            return sprite;
        }

        // ─── Cache Management ───────────────────────────────────

        public static void ClearCache()
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value != null && kvp.Value.texture != null)
                    Object.Destroy(kvp.Value.texture);
                if (kvp.Value != null)
                    Object.Destroy(kvp.Value);
            }
            _cache.Clear();
        }
    }
}
