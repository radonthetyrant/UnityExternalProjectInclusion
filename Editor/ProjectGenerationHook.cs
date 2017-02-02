using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using SyntaxTree.VisualStudio.Unity.Bridge;
using UnityEngine;
using System;

[InitializeOnLoad]
public class ProjectGenerationHook
{

    static ProjectGenerationHook()
    {
        ProjectFilesGenerator.ProjectFileGeneration += (name, content) =>
        {
            try
            {
                Debug.Log("ProjectGenerationHook:" + name);
                var regex = new Regex("\\.CSharp\\.Editor\\.csproj", RegexOptions.RightToLeft);
                if(regex.IsMatch(name))
                    return content;

                string assemblyName = ExternalProjectConfiguration.Instance["assemblyName"];
                string projectFilePath = ExternalProjectConfiguration.Instance["projectFilePath"];
                string projectGuid = ExternalProjectConfiguration.Instance["projectGuid"];

                content = RemoveAssemblyReferenceFromProject(content, assemblyName);
                content = AddProjectReferenceToProject(content, assemblyName, projectFilePath, projectGuid);
                content = AddCopyAssemblyToAssetsPostBuildEvent(content, assemblyName);

                return content;
            } catch (Exception e)
            {
                Debug.LogException(e);
            }
            return content;
        };
    }

    private static string AddCopyAssemblyToAssetsPostBuildEvent(string content, string assemblyName)
    {
        if (content.Contains("PostBuildEvent"))
            return content; // already added

        var signature = new StringBuilder();

        string[] pbe = new string[] {
            string.Format(@"copy /Y ""{0}{1}.*"" ""$(TargetDir)""", ExternalProjectConfiguration.Instance["projectOutputPath"], assemblyName),
            string.Format(@"copy /Y ""$(TargetDir){0}.*"" ""$(ProjectDir)Assets\""", assemblyName),
            string.Format(@"""{0}"" ""$(ProjectDir)Assets\{1}.dll""", ExternalProjectConfiguration.Instance["monoConvertExe"], assemblyName)
        };

        signature.AppendLine("  <PropertyGroup>");
        signature.AppendLine("    <RunPostBuildEvent>Always</RunPostBuildEvent>");
        signature.AppendLine(string.Format(@"    <PostBuildEvent>{0}</PostBuildEvent>", string.Join("\r\n", pbe)));
        signature.AppendLine("  </PropertyGroup>");
        signature.AppendLine("</Project>");

        var regex = new Regex("^</Project>", RegexOptions.Multiline);
        return regex.Replace(content, signature.ToString());
    }

    private static string RemoveAssemblyReferenceFromProject(string content, string assemblyName)
    {
        var regex = new Regex(string.Format(@"^\s*<Reference Include=""{0}"">\r\n\s*<HintPath>.*{0}.dll</HintPath>\r\n\s*</Reference>\r\n", assemblyName), RegexOptions.Multiline);
        return regex.Replace(content, string.Empty);
    }

    private static string AddProjectReferenceToProject(string content, string projectName, string projectFilePath, string projectGuid)
    {
        if (content.Contains(">" + projectName + "<"))
            return content; // already added

        var signature = new StringBuilder();
        signature.AppendLine("  <ItemGroup>");
        signature.AppendLine(string.Format("    <ProjectReference Include=\"{0}\">", projectFilePath));
        signature.AppendLine(string.Format("      <Project>{0}</Project>", projectGuid));
        signature.AppendLine(string.Format("      <Name>{0}</Name>", projectName));
        signature.AppendLine("    </ProjectReference>");
        signature.AppendLine("  </ItemGroup>");
        signature.AppendLine("</Project>");

        var regex = new Regex("^</Project>", RegexOptions.Multiline);
        return regex.Replace(content, signature.ToString());
    }

}
