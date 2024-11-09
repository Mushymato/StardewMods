using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CueSwapGenerator;

internal record GenMethodModel(string OpCode, string TargetCls, string TargetMethod, string OldCue, string NewCue)
{
    internal string OpCode = OpCode;
    internal string TargetCls = TargetCls;
    internal string TargetMethod = TargetMethod;
    internal string OldCue = OldCue;
    internal string NewCue = NewCue;

    internal string GeneratedMethodName
    {
        get
        {
            string methodName = $"TP_{TargetCls}_{TargetMethod}_{OldCue}_{NewCue}";
            return Regex.Replace(methodName, @"[./\\-]", "");
        }
    }

}

internal record GenClassModel(string Namespace, string ClassName, List<GenMethodModel> MethodModels, bool Debug)
{
    internal string Namespace = Namespace;
    internal string ClassName = ClassName;
    internal List<GenMethodModel> MethodModels = MethodModels;
    internal bool Debug = Debug;
};


[Generator]
public class CueSwapTranspilerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx => ctx.AddSource(
            "CueSwapTranspilerAttribute.g.cs",
            SourceText.From(@"namespace CueSwapGenerator;

#pragma warning disable CS9113 // Parameter is unread.
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class CueSwapTranspilerAttribute(
    string opCode,
    string targetCls,
    string targetMethod,
    string oldCue,
    string newCue
) : System.Attribute { }

#pragma warning restore CS9113 // Parameter is unread.
",
            Encoding.UTF8)
        )
        );

        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            "CueSwapGenerator.CueSwapTranspilerAttribute",
            (SyntaxNode node, CancellationToken cancellation) => node is ClassDeclarationSyntax,
            static (GeneratorAttributeSyntaxContext context, CancellationToken cancellation) =>
            {
                var containingClass = context.TargetSymbol.ContainingNamespace;
                bool debug = false;
                List<GenMethodModel> MethodModels = [];
                foreach (var attr in context.Attributes)
                {
                    if (attr.AttributeClass?.Name == "CueSwapTranspilerEnableLoggingAttribute")
                    {
                        debug = true;
                    }
                    else if (attr.AttributeClass?.Name == "CueSwapTranspilerAttribute")
                    {
                        MethodModels.Add(new(
                            (string)attr.ConstructorArguments[0].Value!,
                            (string)attr.ConstructorArguments[1].Value!,
                            (string)attr.ConstructorArguments[2].Value!,
                            (string)attr.ConstructorArguments[3].Value!,
                            (string)attr.ConstructorArguments[4].Value!
                        ));
                    }
                }
                return new GenClassModel(
                    // Note: this is a simplified example. You will also need to handle the case where the type is in a global namespace, nested, etc.
                    Namespace: context.TargetSymbol.ContainingNamespace.Name ?? "GenerateCueSwap",
                    ClassName: context.TargetSymbol.Name,
                    MethodModels: MethodModels,
                    Debug: debug
                );
            }
        );

        context.RegisterSourceOutput(pipeline, static (context, clsModel) =>
        {
            StringBuilder srcBuilder = new(
$$"""
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace {{clsModel.Namespace}};

internal static partial class {{clsModel.ClassName}}
{
    internal static string CheckNewCueExists(string oldCue, string newCue)
    {
#if DEBUG
        ModEntry.Log($"{oldCue}->{newCue} ({Game1.soundBank.Exists(newCue)})");
#endif
        if (Game1.soundBank.Exists(newCue))
            return newCue;
        return oldCue;
    }
""");
            foreach (var model in clsModel.MethodModels)
            {
                srcBuilder.Append(
$$"""
    internal static IEnumerable<CodeInstruction> {{model.GeneratedMethodName}}(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);
            CodeMatch[] methodMatch =
            [
                new(
                    OpCodes.{{model.OpCode}},
                    AccessTools.DeclaredMethod(
                        typeof({{model.TargetCls}}),
                        "{{model.TargetMethod}}"
                    )
                ),
            ];
            matcher
                .Start()
                .MatchStartForward(methodMatch)
                .Repeat(
                    matchAction: (rmatch) =>
                    {
                        int pos = rmatch.Pos;
                        rmatch
                            .MatchEndBackwards([new(OpCodes.Ldstr, "{{model.OldCue}}")])
                            .Advance(1)
                            .InsertAndAdvance(
                                [
                                    new(OpCodes.Ldstr, "{{model.NewCue}}"),
                                    new(
                                        OpCodes.Call,
                                        AccessTools.DeclaredMethod(
                                            typeof({{clsModel.ClassName}}),
                                            nameof(CheckNewCueExists)
                                        )
                                    ),
                                ]
                            );
                        rmatch.MatchStartForward(methodMatch);
                        rmatch.Advance(1);
                    }
                );
            return matcher.Instructions();
        }
        catch (Exception err)
        {
            ModEntry.Log(
                $"Error in Transpiler_doorCreak_doorCreakShippingBin:\n{err}",
                LogLevel.Error
            );
            return instructions;
        }
    }

"""
);
            }
            srcBuilder.Append("}\n");

            var sourceText = SourceText.From(srcBuilder.ToString(), Encoding.UTF8);
            context.AddSource($"{clsModel.ClassName}.g.cs", sourceText);
        });
    }
}
