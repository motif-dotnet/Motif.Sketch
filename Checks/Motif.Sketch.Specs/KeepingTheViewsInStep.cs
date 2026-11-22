namespace Motif.Sketch.Specs;

/// <summary>SPEC §5.3: a change to the context diagram shows up in the sketch straight away, in the same undo step.</summary>
public sealed class KeepingTheViewsInStep
{
    private static async Task<Harness> Started()
    {
        var h = await Harness.Started();
        h.Tasks.RenameElement(h.Tasks.AddElement(ElementType.PrimaryActor), "Customer");
        h.Tasks.RenameElement(h.Tasks.AddElement(ElementType.PrimaryActor), "Staff");
        h.Tasks.StartSketch();
        return h;
    }

    [Fact]
    public async Task An_adapter_still_carrying_its_sources_name_follows_a_rename()
    {
        var h = await Started();

        h.Tasks.RenameElement(h.Element("Customer").Id, "Shopper");

        h.Port("User").Adapters.Select(a => a.Name).Should().Contain("Shopper");
    }

    [Fact]
    public async Task A_renamed_adapter_keeps_its_own_name()
    {
        var h = await Started();
        h.Tasks.RenameAdapter(h.Adapter("Customer").Id, "Web App");

        h.Tasks.RenameElement(h.Element("Customer").Id, "Shopper");

        h.Port("User").Adapters.Select(a => a.Name).Should().Contain("Web App").And.NotContain("Shopper");
    }

    [Fact]
    public async Task Description_and_technology_edits_leave_adapters_alone()
    {
        var h = await Started();

        h.Tasks.SetElementTechnology(h.Element("Customer").Id, "Web browser");
        h.Tasks.DescribeElement(h.Element("Customer").Id, "Buys things");

        h.Adapter("Customer").Should().BeEquivalentTo(new { Technology = (string?)null, Description = (string?)null });
    }

    [Fact]
    public async Task An_added_element_appears_in_the_unplaced_list()
    {
        var h = await Started();

        h.Tasks.AddElement(ElementType.Storage);

        h.UnplacedNames.Should().Equal("Storage 1");
    }

    [Fact]
    public async Task Deleting_an_element_takes_it_out_of_its_adapters_sources_and_flags_the_adapter()
    {
        var h = await Started();
        h.Tasks.RemoveAdapter(h.Adapter("Staff").Id);
        h.Tasks.MergeInto(h.Element("Staff").Id, h.Adapter("Customer").Id);

        h.Tasks.DeleteElement(h.Element("Customer").Id);

        var adapter = h.Adapter("Customer");
        adapter.SourceDeleted.Should().BeTrue();
        adapter.Sources.Select(s => s.Name).Should().Equal("Staff");
        h.Queries.CheckExport().Warnings.Should().ContainSingle(w => w.Contains("deleted"));
    }

    [Fact]
    public async Task The_flag_goes_once_the_adapter_is_edited()
    {
        var h = await Started();
        h.Tasks.DeleteElement(h.Element("Customer").Id);

        h.Tasks.RenameAdapter(h.Adapter("Customer").Id, "Web App");

        h.Adapter("Web App").SourceDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Changing_an_elements_type_keeps_its_placement()
    {
        var h = await Started();

        h.Tasks.SetElementType(h.Element("Customer").Id, ElementType.SecondaryActor);

        h.Adapter("Customer").Sources.Select(s => s.Type).Should().Equal(ElementType.SecondaryActor);
        h.UnplacedNames.Should().BeEmpty();
    }

    [Fact]
    public async Task The_sketchs_follow_up_is_part_of_the_same_undo_step()
    {
        var h = await Started();

        h.Tasks.DeleteElement(h.Element("Customer").Id);
        h.Tasks.Undo();

        h.Adapter("Customer").Should().BeEquivalentTo(new { SourceDeleted = false });
        h.Adapter("Customer").Sources.Select(s => s.Name).Should().Equal("Customer");
    }

    [Fact]
    public async Task The_source_deleted_flag_survives_saving_and_opening()
    {
        var h = await Started();
        h.Tasks.DeleteElement(h.Element("Customer").Id);
        h.Adapter("Customer").SourceDeleted.Should().BeTrue();
        await h.Tasks.SaveAs();
        var saved = h.ProjectFiles.Writes[^1].Text;

        var fresh = new Harness();
        fresh.ProjectFiles.NextOpen = fresh.ProjectFiles.FileOf("again.motifsketch.json", saved);
        (await fresh.Tasks.Open()).Succeeded.Should().BeTrue();

        fresh.Adapter("Customer").SourceDeleted.Should().BeTrue();
        fresh.Adapter("Staff").SourceDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Merging_an_item_in_clears_the_flag()
    {
        var merged = await Started();
        merged.Tasks.DeleteElement(merged.Element("Customer").Id);
        var storage = merged.Tasks.AddElement(ElementType.Storage);
        merged.Adapter("Customer").SourceDeleted.Should().BeTrue();

        merged.Tasks.MergeInto(storage, merged.Adapter("Customer").Id);

        merged.Adapter("Customer").SourceDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Removing_one_of_its_sources_clears_the_flag()
    {
        var removed = await Started();
        removed.Tasks.RemoveAdapter(removed.Adapter("Staff").Id);
        removed.Tasks.MergeInto(removed.Element("Staff").Id, removed.Adapter("Customer").Id);
        removed.Tasks.DeleteElement(removed.Element("Customer").Id);
        removed.Adapter("Customer").SourceDeleted.Should().BeTrue();

        removed.Tasks.RemoveSource(removed.Adapter("Customer").Id, removed.Element("Staff").Id);

        removed.Adapter("Customer").SourceDeleted.Should().BeFalse();
    }
}
