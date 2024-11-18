using System.Text;
using StardewModdingAPI;

namespace StarModsManager.ViewModels.Customs;

public class ModDetailViewModel(IManifest mod, string? addition = null) : ViewModelBase
{
    public string Mod
    {
        get
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"名称： {mod.Name}\n");
            stringBuilder.Append($"唯一ID： {mod.UniqueID}\n");
            stringBuilder.Append($"描述： {mod.Description}\n");
            stringBuilder.Append($"作者： {mod.Author}\n");
            stringBuilder.Append($"版本： {mod.Version}\n");
            if (mod.UpdateKeys.Length > 0)
            {
                stringBuilder.Append("更新代码：");
                foreach (var update in mod.UpdateKeys) stringBuilder.Append($"[{update}]");
                stringBuilder.AppendLine();
            }

            if (mod.ContentPackFor is not null) stringBuilder.Append($"是{mod.ContentPackFor.UniqueID}的内容包");
            if (mod.Dependencies.Length > 0)
            {
                stringBuilder.Append("依赖：\n");
                foreach (var dependency in mod.Dependencies)
                {
                    stringBuilder.Append($"--唯一ID:{dependency.UniqueID}\n");
                    stringBuilder.Append($"  是否必须:{dependency.IsRequired}\n");
                    if (dependency.MinimumVersion is not null)
                        stringBuilder.Append($"  最小版本:{dependency.MinimumVersion}\n");
                }
            }
            
            return stringBuilder.ToString();
        }
    }

    public string? Addition { get; } = addition;
}