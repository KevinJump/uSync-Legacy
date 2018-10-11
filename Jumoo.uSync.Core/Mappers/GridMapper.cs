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
using Umbraco.Core.IO;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Mappers
{
    public class GridMapper : IContentMapper
    {
        IGridConfig gridConfig;
        // List<uSyncContentMapping> usyncMappings;

        public GridMapper()
        {
            var appPlugins = "..\\App_Plugins";
            var configFolder = "..\\Config";
            var debugging = false;

            if (HttpContext.Current != null && HttpContext.Current.Server != null)
            {
                appPlugins = HttpContext.Current.Server.MapPath(SystemDirectories.AppPlugins);
                configFolder = HttpContext.Current.Server.MapPath(SystemDirectories.Config);
                debugging = HttpContext.Current.IsDebuggingEnabled;
            }

            gridConfig = UmbracoConfig.For.GridConfig(
                ApplicationContext.Current.ProfilingLogger.Logger,
                ApplicationContext.Current.ApplicationCache.RuntimeCache,
                new DirectoryInfo(appPlugins),
                new DirectoryInfo(configFolder),
                debugging);

            // usyncMappings = uSyncCoreContext.Instance.Configuration.Settings.ContentMappings;

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
            LogHelper.Debug<GridMapper>("Processing Grid");

            var grid = JsonConvert.DeserializeObject<JObject>(content);
            if (grid == null)
            {
                LogHelper.Warn<GridMapper>("Failed To Deserialize Grid Content: {0}", () => content);
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
                            if (!string.IsNullOrEmpty(mappedVal))
                            {
                                if (IsJson(mappedVal))
                                {
                                    control["value"] = JToken.Parse(mappedVal);
                                }
                                else
                                {
                                    control["value"] = mappedVal;
                                }
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(grid, Formatting.Indented);
        }


        private string ProcessControl(JObject control, bool import)
        {
            LogHelper.Debug<GridMapper>("Processing: {0}", ()=> control.ToString());
            var mapper = GetEditorMapping(control.Value<JObject>("editor"));

            if (mapper == null)
                return string.Empty;

            var value = control.Value<object>("value");
            LogHelper.Debug<GridMapper>("#####\nBefore: Control Value: {0} {1}\n#####", () => value.GetType(), () => value);

            var mappedValue = value.ToString();
            if (import)
            {
                mappedValue = mapper.GetImportValue(0, value.ToString());
            }
            else
            {
                mappedValue = mapper.GetExportValue(0, value.ToString());
            }

            if (!IsJson(mappedValue))
            {
                control["value"] = mappedValue;
            }
            else
            {
                var mappedJson = JToken.Parse(mappedValue);
                if (mappedJson != null)
                {
                    control["value"] = mappedJson;
                }

            }

            LogHelper.Debug<GridMapper>("#####\nAfter: Control Value: {0} {1}\n#####", () => value.GetType(), () => control.Value<object>("value").ToString());
            return mappedValue;
        }

        private IContentMapper GetEditorMapping(JObject editor)
        {
            if (editor == null)
                return null;

            var alias = editor.Value<string>("alias");
            var uSyncAlias = string.Format("grid.{0}", alias);

            LogHelper.Debug<GridMapper>("Getting Mapper for {0}", () => uSyncAlias);

            // var mapping = usyncMappings.SingleOrDefault(x => x.EditorAlias == uSyncAlias);
            var mapper = ContentMapperFactory.GetMapper(uSyncAlias);
            if (mapper == null)
            {
                LogHelper.Debug<GridMapper>("Not a normal mapper - look by view ?");
                // get by view name. 
                var config = gridConfig.EditorsConfig.Editors
                    .SingleOrDefault(x => x.Alias == alias);

                if (config != null)
                {
                    LogHelper.Debug<GridMapper>("Getting Mapper by View: {0}", () => config.View);
                    mapper = ContentMapperFactory.GetByViewName(config.View);
                }
            }

            return mapper;
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
