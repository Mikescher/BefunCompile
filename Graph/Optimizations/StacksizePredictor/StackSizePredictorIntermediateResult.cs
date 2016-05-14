namespace BefunCompile.Graph.Optimizations.StacksizePredictor
{
	enum StackSizePredictorIntermediateResult
	{
		FinishedLeaf,
		FinishedStableLoop,
		ProcessedVertex,
		UnboundedGrowth,
	}
}
