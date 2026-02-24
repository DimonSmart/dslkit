using DSLKIT.Parser;

namespace DSLKIT.Visualizer.App.Visualization;

public interface IGrammarSnapshotMapper
{
    GrammarSnapshotDto Map(IGrammar grammar);
}
