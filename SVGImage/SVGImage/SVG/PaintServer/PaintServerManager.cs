using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media;
using System.Xml;

namespace SVGImage.SVG.PaintServer
{
    public class PaintServerManager
    {
        private static Dictionary<string, Color> m_knownColors = null;

        private Dictionary<string, PaintServer> m_servers = new Dictionary<string, PaintServer>();

        public PaintServer Create(XmlNode node)
        {
            if (node.Name == SVGTags.sLinearGradient)
            {
                string id = XmlUtil.AttrValue(node, "id");
                if (this.m_servers.ContainsKey(id) == false) this.m_servers[id] = new LinearGradientColorPaintServerPaintServer(this, node);
                return this.m_servers[id];
            }
            if (node.Name == SVGTags.sRadialGradient)
            {
                string id = XmlUtil.AttrValue(node, "id");
                if (this.m_servers.ContainsKey(id) == false) this.m_servers[id] = new RadialGradientColorPaintServerPaintServer(this, node);
                return this.m_servers[id];
            }
            return null;
        }

        public PaintServer Parse(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (value == "none") return null;
            if (value[0] == '#') return this.ParseSolidColor(value);
            PaintServer result = null;
            if (this.m_servers.TryGetValue(value, out result)) return result;
            if (value.StartsWith("url"))
            {
                string id = ShapeUtil.ExtractBetween(value, '(', ')');
                if (id.Length > 0 && id[0] == '#') id = id.Substring(1);
                this.m_servers.TryGetValue(id, out result);
                return result;
            }
            return this.ParseKnownColor(value);
        }

        public static Color ParseHexColor(string value)
        {
            // format is #xxFF00FF where xx is optional (the a value)
            // if format ix #rgb then the values are replicated #rrggbb
            int start = 0;
            if (value[start] == '#') start++;

            uint u = Convert.ToUInt32(value.Substring(start), 16);
            if (value.Length <= 4)
            {
                uint newval = 0;
                newval |= (u & 0x000f00) << 12;
                newval |= (u & 0x000f00) << 8;
                newval |= (u & 0x0000f0) << 8;
                newval |= (u & 0x0000f0) << 4;
                newval |= (u & 0x00000f) << 4;
                newval |= (u & 0x00000f);
                u = newval;
            }
            byte a = (byte)((u & 0xff000000) >> 24);
            byte r = (byte)((u & 0x00ff0000) >> 16);
            byte g = (byte)((u & 0x0000ff00) >> 8);
            byte b = (byte)(u & 0x000000ff);
            if (a == 0) a = 255;
            return Color.FromArgb(a, r, g, b);
        }

        public static Color KnownColor(string value)
        {
            LoadKnownColors();
            if (m_knownColors.ContainsKey(value)) return m_knownColors[value];
            return Colors.Black;
        }

        private SolidColorPaintServer ParseSolidColor(string value)
        {
            string id = "_solid" + value;
            PaintServer result;
            if (this.m_servers.TryGetValue(id, out result)) return result as SolidColorPaintServer;
            result = new SolidColorPaintServer(this, ParseHexColor(value));
            this.m_servers[id] = result;
            return result as SolidColorPaintServer;
        }

        private SolidColorPaintServer ParseKnownColor(string value)
        {
            LoadKnownColors();
            PaintServer result;
            if (this.m_servers.TryGetValue(value, out result)) return result as SolidColorPaintServer;
            Color c;
            if (m_knownColors.TryGetValue(value, out c))
            {
                result = new SolidColorPaintServer(this, c);
                this.m_servers[value] = result;
                return result as SolidColorPaintServer;
            }
            return null;
        }

        private static void LoadKnownColors()
        {
            if (m_knownColors == null) m_knownColors = new Dictionary<string, Color>();
            if (m_knownColors.Count == 0)
            {
                PropertyInfo[] propinfos = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static);
                foreach (PropertyInfo info in propinfos)
                {
                    if (info.PropertyType == typeof(Color)) m_knownColors[info.Name.ToLower()] = (Color)info.GetValue(typeof(Color), null);
                }
            }
        }
    }
}