using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Mappers
{
    public class GridMapper : IContentMapper
    {
        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            var grid = JsonConvert.DeserializeObject<dynamic>(value);
            if (grid == null)
                return value;

            foreach (var section in grid.sections)
            {
                foreach (var row in section.rows)
                {
                    foreach (var area in row.areas)
                    {
                        foreach (var control in area.controls)
                        {
                            if (control.value != null)
                            {
                                // do some mapping on the value...
                                if (control.editor != null)
                                {
                                    var val = control.value;
                                    var editorType = "Umbraco.TinyMCEv3"; // both media and rte use the same mapper.
                                    uSyncContentMapping mapping =
                                        uSyncCoreContext.Instance.Configuration.Settings.ContentMappings.SingleOrDefault(x => x.EditorAlias == editorType);

                                    if (mapping != null)
                                    {
                                        IContentMapper mapper = ContentMapperFactory.GetMapper(mapping);
                                        if (mapper != null)
                                        {
                                            switch ((string)control.editor.alias)
                                            {
                                                case "rte":
                                                    LogHelper.Debug<Events>("RTE: {0}", () => control.value);
                                                    control.value = mapper.GetExportValue(0, (string)control.value);
                                                    break;
                                                case "media":
                                                    if (control.value.id != null)
                                                    {
                                                        LogHelper.Debug<Events>("Media: {0}", () => control.value.id);
                                                        control.value.id = mapper.GetExportValue(0, (string)control.value.id);
                                                    }
                                                    break;
                                                default:
                                                    // we don't mapp the other ones.
                                                    LogHelper.Debug<Events>("{0} mapping not supported (or needed?)", () => control.editor.alias);
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(grid, Formatting.Indented);
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            var grid = JsonConvert.DeserializeObject<dynamic>(content);
            if (grid == null)
                return content;

            foreach (var section in grid.sections)
            {
                foreach (var row in section.rows)
                {
                    foreach (var area in row.areas)
                    {
                        foreach (var control in area.controls)
                        {
                            if (control.value != null)
                            {
                                // do some mapping on the value...
                                if (control.editor != null)
                                {
                                    var val = control.value;
                                    var editorType = "Umbraco.TinyMCEv3"; // both media and rte use the same mapper.
                                    uSyncContentMapping mapping =
                                        uSyncCoreContext.Instance.Configuration.Settings.ContentMappings.SingleOrDefault(x => x.EditorAlias == editorType);

                                    if (mapping != null)
                                    {
                                        IContentMapper mapper = ContentMapperFactory.GetMapper(mapping);
                                        if (mapper != null)
                                        {
                                            switch ((string)control.editor.alias)
                                            {
                                                case "rte":
                                                    LogHelper.Debug<Events>("RTE: {0}", () => control.value);
                                                    control.value = mapper.GetImportValue(0, (string)control.value);
                                                    break;
                                                case "media":
                                                    if (control.value.id != null)
                                                    {
                                                        LogHelper.Debug<Events>("Media: {0}", () => control.value.id);
                                                        control.value.id = mapper.GetImportValue(0, (string)control.value.id);
                                                    }
                                                    break;
                                                default:
                                                    // we don't mapp the other ones.
                                                    LogHelper.Debug<Events>("{0} mapping not supported (or needed?)", () => control.editor.alias);
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(grid, Formatting.Indented);
        }
    }
}
