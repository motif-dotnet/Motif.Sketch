namespace Motif.Sketch.Specs;

public sealed class Files
{
    [Fact]
    public async Task Save_then_open_gives_an_identical_file()
    {
        var h = await Harness.WithExample();
        h.Tasks.AddElement(ElementType.SecondaryActor);
        await h.Tasks.SaveAs();
        var saved = h.ProjectFiles.Writes[^1].Text;

        h.ProjectFiles.NextOpen = h.ProjectFiles.FileOf("again.motifsketch.json", saved);
        (await h.Tasks.Open()).Succeeded.Should().BeTrue();
        await h.Tasks.SaveAs();

        h.ProjectFiles.Writes[^1].Text.Should().Be(saved);
    }

    [Fact]
    public async Task The_file_is_indented_camelCase_json_and_the_sketch_is_absent_until_started()
    {
        var h = await Harness.Started();
        await h.Tasks.SaveAs();

        var json = h.ProjectFiles.Writes[^1].Text;
        json.Should().StartWith("{\n  \"schemaVersion\": 1,\n  \"title\": \"Untitled\",\n  \"contextDiagram\": {");
        json.Should().Contain("\"type\": \"system\"").And.NotContain("\"sketch\"");
    }

    [Fact]
    public async Task The_code_root_is_saved_on_the_system_and_left_out_when_blank()
    {
        var h = await Harness.Started();
        await h.Tasks.SaveAs();
        h.ProjectFiles.Writes[^1].Text.Should().NotContain("codeRoot");

        h.Tasks.SetCodeRoot("Shop.Orders");
        await h.Tasks.SaveAs();
        var saved = h.ProjectFiles.Writes[^1].Text;
        saved.Should().Contain("\"codeRoot\": \"Shop.Orders\"");

        h.ProjectFiles.NextOpen = h.ProjectFiles.FileOf("again.motifsketch.json", saved);
        (await h.Tasks.Open()).Succeeded.Should().BeTrue();
        h.System.CodeRoot.Should().Be("Shop.Orders");
    }

    [Fact]
    public async Task A_Seam_is_saved_on_its_adapter_and_left_out_when_there_is_none()
    {
        var h = await Harness.WithExample();
        h.ClearSeams();
        await h.Tasks.SaveAs();
        h.ProjectFiles.Writes[^1].Text.Should().NotContain("\"seam\"");

        h.Tasks.SetAdapterSeam(h.Adapter("IndexedDB").Id, "Browser Storage");
        await h.Tasks.SaveAs();
        var saved = h.ProjectFiles.Writes[^1].Text;
        saved.Should().Contain("\"seam\": \"Browser Storage\"");

        h.ProjectFiles.NextOpen = h.ProjectFiles.FileOf("again.motifsketch.json", saved);
        (await h.Tasks.Open()).Succeeded.Should().BeTrue();
        h.Adapter("IndexedDB").Seam.Should().Be("Browser Storage");
    }

    [Fact]
    public async Task A_file_with_a_Seam_on_a_fake_doesnt_open()
    {
        var h = await Harness.Started();
        var json = Example.Json().Replace(
            "\"name\": \"Fake for copying\",", "\"name\": \"Fake for copying\",\n            \"seam\": \"Clipboard\",");
        json.Should().Contain("\"seam\"");
        h.ProjectFiles.NextOpen = h.ProjectFiles.FileOf("bad.motifsketch.json", json);

        var outcome = await h.Tasks.Open();

        outcome.Succeeded.Should().BeFalse();
        outcome.Problems.Should().Contain(p => p.Contains("Fake for copying") && p.Contains("Seam"));
    }

    [Fact]
    public async Task In_save_in_place_browsers_Save_writes_back_to_the_same_file()
    {
        var h = await Harness.Started();
        h.ProjectFiles.NextOpen = h.ProjectFiles.FileOf("shop.motifsketch.json", Example.Json());
        await h.Tasks.Open();

        h.Tasks.AddElement(ElementType.Storage);
        var outcome = await h.Tasks.Save();

        outcome.Should().BeEquivalentTo(new { Succeeded = true, Title = "Saved to shop.motifsketch.json" });
        var write = h.ProjectFiles.Writes.Should().ContainSingle().Which;
        (write.Name, write.Downloaded).Should().Be(("shop.motifsketch.json", false));
        h.Queries.FileName.Should().Be("shop.motifsketch.json");
        h.Queries.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task In_save_in_place_browsers_theres_nothing_to_say_about_how_Save_works()
    {
        var h = await Harness.Started();

        h.Queries.HowProjectsAreSaved.Should().BeEmpty();
    }

    [Fact]
    public async Task In_other_browsers_the_user_is_told_Save_downloads_a_copy()
    {
        var h = await Harness.Started();
        h.ProjectFiles.CanWriteInPlace = false;

        h.Queries.HowProjectsAreSaved.Should().Be(
            "This browser can't save in place, so Save downloads a copy. Edge and Chrome save back to the same file.");
    }

    [Fact]
    public async Task Save_as_picks_a_new_file_and_later_saves_go_there()
    {
        var h = await Harness.Started();
        h.ProjectFiles.NextSaveAsName = "mine.motifsketch.json";

        await h.Tasks.SaveAs();
        h.Tasks.AddElement(ElementType.Storage);
        await h.Tasks.Save();

        h.ProjectFiles.Writes.Select(w => w.Name).Should().Equal("mine.motifsketch.json", "mine.motifsketch.json");
        h.ProjectFiles.Writes.Select(w => w.Handle).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task In_other_browsers_Save_downloads_a_copy()
    {
        var h = await Harness.Started();
        h.ProjectFiles.CanWriteInPlace = false;
        h.Tasks.RenameProject("Shop");

        await h.Tasks.Save();
        var outcome = await h.Tasks.Save();

        outcome.Title.Should().Be("Downloaded Shop.motifsketch.json");
        h.ProjectFiles.Writes.Should().HaveCount(2).And.OnlyContain(w => w.Downloaded && w.Name == "Shop.motifsketch.json");
        h.Queries.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task A_cancelled_save_leaves_the_work_unsaved()
    {
        var h = await Harness.Started();
        h.Tasks.AddElement(ElementType.Storage);
        h.ProjectFiles.NextSaveAsName = null;

        (await h.Tasks.Save()).Cancelled.Should().BeTrue();

        h.Queries.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task A_failed_save_says_why()
    {
        var h = await Harness.Started();
        h.ProjectFiles.Fail = true;

        var outcome = await h.Tasks.Save();

        outcome.Should().BeEquivalentTo(new { Succeeded = false, Title = "The project couldn't be saved" });
        outcome.Problems.Should().ContainSingle().Which.Should().Contain("disk is full");
    }

    [Fact]
    public async Task Edits_mark_unsaved_changes_and_New_starts_clean()
    {
        var h = await Harness.Started();
        h.Queries.HasUnsavedChanges.Should().BeFalse();

        h.Tasks.AddElement(ElementType.Storage);
        h.Queries.HasUnsavedChanges.Should().BeTrue();

        await h.Tasks.NewProject();
        h.Queries.Should().BeEquivalentTo(new { HasUnsavedChanges = false, CanUndo = false, FileName = (string?)null });
    }

    [Fact]
    public async Task A_hand_written_file_without_positions_is_laid_out()
    {
        var h = await Harness.Started();
        h.ProjectFiles.NextOpen = h.ProjectFiles.FileOf("hand.motifsketch.json", """
            { "schemaVersion": 1, "title": "Hand", "contextDiagram": {
              "elements": [ { "id": "s", "type": "system", "name": "Sys" }, { "id": "a", "type": "primaryActor", "name": "Person" } ],
              "relationships": [ { "id": "r", "from": "a", "to": "s", "label": "Uses" } ] } }
            """);

        (await h.Tasks.Open()).Succeeded.Should().BeTrue();

        h.Element("Person").Box.Right.Should().BeLessThan(h.System.Box.X);
    }

    [Theory]
    [InlineData("not json at all", "can't be read")]
    [InlineData("""{ "schemaVersion": 2, "contextDiagram": { "elements": [] } }""", "schema version 2")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "planet", "name": "X" } ] } }""", "unknown type 'planet'")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [] } }""", "exactly one system")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "s", "type": "storage", "name": "B" } ], "relationships": [ { "id": "r", "from": "s", "to": "s" } ] } }""", "used more than once")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" } ], "relationships": [ { "id": "r", "from": "s", "to": "ghost" } ] } }""", "isn't an element")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "b", "type": "storage", "name": "B" } ], "relationships": [] } }""", "exactly one relationship")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "b", "type": "storage", "name": "B" }, { "id": "c", "type": "storage", "name": "C" } ], "relationships": [ { "id": "r1", "from": "s", "to": "b" }, { "id": "r2", "from": "b", "to": "c" } ] } }""", "doesn't have the system at one end")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "a", "type": "primaryActor", "name": "Ann", "codeRoot": "Ann" } ], "relationships": [ { "id": "r", "from": "a", "to": "s" } ] } }""", "only the system can have one")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" } ] }, "sketch": { "ports": [] } }""", "no User port")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" } ] }, "sketch": { "ports": [ { "id": "u", "name": "User", "side": "driving", "isUser": true }, { "id": "p", "name": "P", "side": "driven", "adapters": [ { "id": "x", "name": "Specs", "kind": "checks" } ] } ] } }""", "driving ports only")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" } ] }, "sketch": { "ports": [ { "id": "u", "name": "User", "side": "driving", "isUser": true, "adapters": [ { "id": "x", "name": "X", "kind": "real", "sources": ["nope"] } ] } ] } }""", "isn't an element of the context diagram")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "a", "type": "primaryActor", "name": "Ann" } ], "relationships": [ { "id": "r", "from": "a", "to": "s" } ] }, "sketch": { "ports": [ { "id": "u", "name": "User", "side": "driving", "isUser": true, "adapters": [ { "id": "x", "name": "X", "kind": "real", "sources": ["a"] }, { "id": "y", "name": "Y", "kind": "real", "sources": ["a"] } ] } ] } }""", "more than one adapter")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "a", "type": "primaryActor", "name": "Ann" } ], "relationships": [ { "id": "r", "from": "a", "to": "a" } ] } }""", "links an element to itself")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" } ] }, "sketch": { "ports": [ { "id": "u", "name": "User", "side": "driven", "isUser": true } ] } }""", "must be on the driving side")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" } ] }, "sketch": { "ports": [ { "id": "u", "name": "User", "side": "driving", "isUser": true }, { "id": "v", "name": "User 2", "side": "driving", "isUser": true } ] } }""", "2 User ports")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "", "type": "storage", "name": "B" } ], "relationships": [ { "id": "r", "from": "s", "to": "" } ] } }""", "has no id")]
    public async Task An_invalid_file_is_reported_and_leaves_the_current_work_untouched(string text, string problem)
    {
        var h = await Harness.WithExample();
        var before = h.Queries.ProjectJsonText;
        h.ProjectFiles.NextOpen = h.ProjectFiles.FileOf("bad.motifsketch.json", text);

        var outcome = await h.Tasks.Open();

        outcome.Should().BeEquivalentTo(new { Succeeded = false, Title = "That file can't be opened" });
        outcome.Problems.Should().Contain(p => p.Contains(problem));
        h.Queries.ProjectJsonText.Should().Be(before);
    }

    [Fact]
    public async Task Every_problem_in_a_file_is_listed()
    {
        var h = await Harness.Started();
        h.ProjectFiles.NextOpen = h.ProjectFiles.FileOf("bad.motifsketch.json",
            """{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "b", "type": "storage", "name": "" } ] } }""");

        var outcome = await h.Tasks.Open();

        outcome.Problems.Should().Equal(
            "There must be exactly one system, but there are 0.",
            "Element 'b' has no name.",
            "'' must have exactly one relationship, with the system, but has 0.");
    }
}
