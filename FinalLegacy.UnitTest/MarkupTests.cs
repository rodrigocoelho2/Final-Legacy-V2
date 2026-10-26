using FinalLegacy.Library.Localization;
using FinalLegacy.Library.Text;

namespace FinalLegacy.UnitTest;

public class MarkupTests
{
    [Fact]
    public void Colours_resets_and_pauses_are_tokens()
    {
        var tokens = Markup.Parse("[amber]Rodas[/][pause]... ok").ToList();

        Assert.Equal(
            new[] { MarkupTokenKind.Color, MarkupTokenKind.Text, MarkupTokenKind.Reset, MarkupTokenKind.Pause, MarkupTokenKind.Text },
            tokens.Select(t => t.Kind));
        Assert.Equal("amber", tokens[0].Value);
        Assert.Equal("... ok", tokens[4].Value);
    }

    [Fact]
    public void Strip_leaves_the_text_as_it_reads()
    {
        Assert.Equal("Bem-vindo ao Final Legacy!", Markup.Strip("Bem-vindo ao [gold]Final Legacy[/]!"));
    }

    [Fact]
    public void Unknown_tags_and_escaped_brackets_stay_as_text()
    {
        Assert.Equal("[1] Sim [x]", Markup.Strip("[[1] Sim [x]"));
    }

    [Fact]
    public void A_name_with_brackets_cannot_inject_colours()
    {
        var text = Localizer.Fill("[amber]{name}[/]", ("name", Markup.Escape("[red]Evil")));

        Assert.Equal("[red]Evil", Markup.Strip(text));
    }

    [Fact]
    public void Placeholders_are_filled_and_unknown_ones_kept()
    {
        Assert.Equal("6 de dano {x}", Localizer.Fill("{damage} de dano {x}", ("damage", 6)));
        Assert.Equal("O Esqueleto", Localizer.Capitalize("o Esqueleto"));
    }
}
