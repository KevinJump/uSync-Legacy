using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Configuration.Grid;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Mappers
{
    public class EnahncedGridMapper : IContentMapper
    {
        IGridConfig gridConfig;
        List<uSyncContentMapping> usyncMappings;

        public EnahncedGridMapper()
        {
            gridConfig = UmbracoConfig.For.GridConfig(
                ApplicationContext.Current.ProfilingLogger.Logger,
                ApplicationContext.Current.ApplicationCache.RuntimeCache,
                new DirectoryInfo(HttpContext.Current.Server.MapPath(SystemDirectories.AppPlugins)),
                new DirectoryInfo(HttpContext.Current.Server.MapPath(SystemDirectories.Config)),
                HttpContext.Current.IsDebuggingEnabled);

            usyncMappings = uSyncCoreContext.Instance.Configuration.Settings.ContentMappings;

        }

        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            return ProcessGrid(value, false);
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            return ProcessGrid(content, true);
        }

        private string ProcessGrid(string content, bool import)
        {
            LogHelper.Debug<EnahncedGridMapper>("Processing Grid");

            var grid = JsonConvert.DeserializeObject<JObject>(content);
            if (grid == null)
            {
                LogHelper.Warn<EnahncedGridMapper>("Failed To Deserialize Grid Content: {0}", () => content);
                return content;
            }

            var sections = GetArray(grid, "sections");
            foreach(var section in sections.Cast<JObject>())
            {
                var rows = GetArray(section, "rows");
                foreach(var row in rows.Cast<JObject>())
                {
                    var areas = GetArray(row, "areas");
                    foreach(var area in areas.Cast<JObject>())
                    {
                        var controls = GetArray(area, "controls");
                        foreach (var control in controls.Cast<JObject>())
                        {
                            var mappedVal = ProcessControl(control, import);
                            if (IsJson(mappedVal))
                            {
                                control["value"] = JToken.Parse(mappedVal);
                            }
                            else {
                                control["value"] = mappedVal;
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(grid, Formatting.Indented);
        }


        private string ProcessControl(JObject control, bool import)
        {
            LogHelper.Debug<EnahncedGridMapper>("Processing: {0}", ()=> control.ToString());
            var mapper = GetEditorMapping(control.Value<JObject>("editor"));

            if (mapper == null)
                return control.ToString();

            var value = control.Value<object>("value");


            return control.ToString();
        }

        private IContentMapper GetEditorMapping(JObject editor)
        {
            if (editor == null)
                return null;

            var alias = editor.Value<string>("alias");
            var uSyncAlias = string.Format("grid.{0}", alias);

            var mapping = usyncMappings.SingleOrDefault(x => x.EditorAlias == uSyncAlias);
            if (mapping == null)
            {
                // lookup in the grid config
                var config = gridConfig.EditorsConfig.Editors
                    .SingleOrDefault(x => x.Alias == alias);

                // lookup by view 
                if (config != null)
                {
                    mapping = usyncMappings
                        .SingleOrDefault(
                            x => !string.IsNullOrEmpty(x.View)
                            && config.View.IndexOf(x.View, StringComparison.InvariantCultureIgnoreCase)>0);
                }
            }

            if (mapping != null)
            {
                var mapper = ContentMapperFactory.GetMapper(mapping);
                return mapper;
            }

            return null;
        }

        private JArray GetArray(JObject obj, string propertyName)
        {
            JToken token;
            if (obj.TryGetValue(propertyName, out token))
            {
                var asArray = token as JArray;
                return asArray ?? new JArray();
            }
            return new JArray();
        }
        private bool IsJson(string val)
        {
            val = val.Trim();
            return (val.StartsWith("{") && val.EndsWith("}"))
                || (val.StartsWith("[") && val.EndsWith("]"));
        }

    }
}
