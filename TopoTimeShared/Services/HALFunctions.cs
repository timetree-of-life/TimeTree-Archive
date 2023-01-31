using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.Drawing;
using System.Data;
using TimeTreeShared;
using TimeTreeShared.Services;

namespace TopoTimeShared
{
    public class UPGMAMatrix
    {
        public Dictionary<(int, int), Dictionary<int, Divergence>> PairDivergenceDictionary = new Dictionary<(int, int), Dictionary<int, Divergence>>();
        public Dictionary<int, TopoTimeNode> RepresentativeDictionary = new Dictionary<int, TopoTimeNode>();

        public UPGMAMatrix()
        {

        }

        public UPGMAMatrix(int nodeCapacity, int partitionCapacity)
        {
            PairDivergenceDictionary = new Dictionary<(int, int), Dictionary<int, Divergence>>(partitionCapacity);
            RepresentativeDictionary = new Dictionary<int, TopoTimeNode>(nodeCapacity);
        }

        public Dictionary<int, Divergence> this[int row, int column]
        {
            get
            {
                PairDivergenceDictionary.TryGetValue((Math.Min(row, column), Math.Max(row, column)), out Dictionary<int, Divergence> divergenceSet);
                return divergenceSet;
            }
        }

        public TopoTimeNode this[int index]
        {
            get
            {
                RepresentativeDictionary.TryGetValue(index, out TopoTimeNode representative);
                return representative;
            }
        }

        public void AddDivergence(int indexA, int indexB, TopoTimeNode representativeA, TopoTimeNode representativeB, double divergenceTime, int citation, int phylogeny_node, bool topologyConflict)
        {
            int a = Math.Min(indexA, indexB);
            int b = Math.Max(indexA, indexB);

            if (a == b)
                throw new Exception("Cannot add a pair of identical nodes");

            if (!PairDivergenceDictionary.ContainsKey((a, b)))
                PairDivergenceDictionary[(a, b)] = new Dictionary<int, Divergence>();

            Dictionary<int, Divergence> pairDivergence = PairDivergenceDictionary[(a, b)];

            if (pairDivergence.ContainsKey(citation))
                pairDivergence[citation].AddDivergence(divergenceTime, phylogeny_node, topologyConflict);
            else
                pairDivergence[citation] = new Divergence(divergenceTime, phylogeny_node, citation, topologyConflict);

            RepresentativeDictionary[indexA] = representativeA;
            RepresentativeDictionary[indexB] = representativeB;
        }

        public bool TopologyConflict(int indexA, int indexB)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = this[indexA, indexB];
            if (divergences == null || divergences.Count() == 0)
                return true;

            // first, pick out divergences with no time but still define topology
            // these take precedence when all other divergence time entries show topology conflict

            bool hasZeroConcordantTime = false;
            bool hasOtherConcordantTime = false;
            bool hasAllConcordantTimes = true;

            foreach (Divergence div in divergences.Select(x => x.Value))
            {
                if (div.IsConflict)
                {
                    hasAllConcordantTimes = false;
                }
                else
                {
                    if (div.DivergenceTime <= 0)
                    {
                        hasZeroConcordantTime = true;
                    }
                    else if (div.DivergenceTime > 0)
                    {
                        hasOtherConcordantTime = true;
                    }
                }
            }

            if (hasZeroConcordantTime && !hasOtherConcordantTime)
                return false;
            else
                return !hasAllConcordantTimes;
        }

        public bool HasConcordantDivergences(int indexA, int indexB)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = this[indexA, indexB];
            IEnumerable<KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => x.Value.IsConflict == false);

            return noConflictDivergences.Any();
        }

        public IEnumerable<KeyValuePair<int, Divergence>> ValidDivergences(int indexA, int indexB)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = this[indexA, indexB];

            if (divergences != null)
            {
                IEnumerable<KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => x.Value.IsConflict == false);

                // return topology-conflicted times only if no other such data exists

                if (noConflictDivergences.Any())
                {
                    foreach (KeyValuePair<int, Divergence> divergence in noConflictDivergences)
                        yield return divergence;
                }
                else
                {
                    foreach (KeyValuePair<int, Divergence> divergence in divergences)
                        yield return divergence;
                }
            }
        }

        /*
        public double distance(int indexA, int indexB, bool useMedian = false, bool useOnlyConcordant = false, int guideTree = 0)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = this[indexA, indexB];

            if (divergences == null || !divergences.Any())
                return Double.NaN;

            // if guide tree is specified, only exclude non-guide times when guide tree is not present
            bool useGuideTree = guideTree != 0 && divergences.Any(x => x.Value.CitationID == guideTree && !x.Value.IsConflict);

            // first, pick out divergences with no time but still define topology
            // these take precedence when all other divergence time entries show topology conflict

            bool hasZeroConcordantTime = false;
            bool hasOtherConcordantTime = false;
            //bool hasAllConcordantTimes = true;

            foreach (Divergence div in divergences.Select(x => x.Value))
            {
                if (!div.IsConflict)
                {
                    if (div.DivergenceTime <= 0 && !div.IsConflict)
                    {
                        hasZeroConcordantTime = true;
                    }
                    else if (div.DivergenceTime > 0 && !div.IsConflict)
                    {
                        hasOtherConcordantTime = true;
                    }
                }
            }

            if (hasZeroConcordantTime && !hasOtherConcordantTime)
                return 0;
            else
            {
                IEnumerable<double> nonZeroTimes;

                if (useOnlyConcordant)
                    nonZeroTimes = divergences.Where(x => !x.Value.IsConflict).Select(x => x.Value.DivergenceTime).Where(y => y > 0);
                else
                    nonZeroTimes = divergences.Select(x => x.Value.DivergenceTime).Where(y => y > 0);

                if (!nonZeroTimes.Any())
                    return 0;
                else if (useMedian && nonZeroTimes.Count() > 4)
                    return nonZeroTimes.Median();
                else
                    return nonZeroTimes.Average();
            }
        }
        */

        public double distance(int indexA, int indexB, bool useMedian = false, bool useOnlyConcordant = false, int guideTree = 0)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = this[indexA, indexB];

            if (guideTree != 0)
                divergences = divergences.Where(x => x.Value.CitationID == guideTree);

            if (divergences == null || !divergences.Any())
                return Double.NaN;

            // if guide tree is specified, only exclude non-guide times when guide tree is not present
            bool useGuideTree = guideTree != 0 && divergences.Any(x => x.Value.CitationID == guideTree && !x.Value.IsConflict);

            // first, pick out divergences with no time but still define topology
            // these take precedence when all other divergence time entries show topology conflict

            bool hasZeroConcordantTime = false;
            bool hasOtherConcordantTime = false;
            //bool hasAllConcordantTimes = true;

            foreach (Divergence div in divergences.Select(x => x.Value))
            {
                if (useGuideTree && div.CitationID != guideTree)
                    continue;

                if (!div.IsConflict)
                {
                    if (div.DivergenceTime <= 0 && !div.IsConflict)
                    {
                        hasZeroConcordantTime = true;
                    }
                    else if (div.DivergenceTime > 0 && !div.IsConflict)
                    {
                        hasOtherConcordantTime = true;
                    }
                }
            }

            // new behavior 11-23-2022, stops here and no longer yields ANY discordant times
            if (hasZeroConcordantTime && !hasOtherConcordantTime && useOnlyConcordant)
                return 0;
            else
            {
                IEnumerable<double> nonZeroTimes;

                if (useOnlyConcordant)
                    nonZeroTimes = divergences.Where(x => !x.Value.IsConflict && (!useGuideTree || guideTree == x.Value.CitationID)).Select(x => x.Value.DivergenceTime).Where(y => y > 0);
                else
                    nonZeroTimes = divergences.Where(x => (!useGuideTree || guideTree == x.Value.CitationID)).Select(x => x.Value.DivergenceTime).Where(y => y > 0);

                if (!nonZeroTimes.Any())
                    return 0;
                else if (useMedian && nonZeroTimes.Count() > 4)
                    return nonZeroTimes.Median();
                else
                    return nonZeroTimes.Average();
            }
        }

        public int countAgreement(int indexA, int indexB)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = this[indexA, indexB];

            if (divergences == null || !divergences.Any())
                return -1;

            IEnumerable<KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => x.Value.IsConflict == false);
            return noConflictDivergences.Count();

        }

        public double percentSupport(int indexA, int indexB)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = this[indexA, indexB];            

            if (divergences == null || !divergences.Any())
                return 0;

            IEnumerable<KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => x.Value.IsConflict == false);
            return (double)noConflictDivergences.Count() / (double)divergences.Count();
        }

        public double percentSupport(int indexA, int indexB, int guideTree = 0)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = this[indexA, indexB];
            if (guideTree != 0)
                divergences = divergences.Where(x => x.Value.CitationID == guideTree);

            if (divergences == null || !divergences.Any())
                return 0;

            // if guide tree is specified, only exclude non-guide times when guide tree is not present
            bool useGuideTree = guideTree != 0 && divergences.Any(x => x.Value.CitationID == guideTree && x.Value.IsConflict == false);

            double totalDivergences = divergences.Count();
            if (useGuideTree)
                totalDivergences = divergences.Where(x => x.Value.CitationID == guideTree && x.Value.IsConflict == false).Count();

            IEnumerable<KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => (!useGuideTree || guideTree == x.Value.CitationID) && x.Value.IsConflict == false);
            return (double)noConflictDivergences.Count() / totalDivergences;
        }

        public int supportingStudies(int indexA, int indexB)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = this[indexA, indexB];

            if (divergences == null || !divergences.Any())
                return 0;

            IEnumerable<KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => x.Value.IsConflict == false);
            return noConflictDivergences.Count();
        }

        public int totalStudies(int indexA, int indexB)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = this[indexA, indexB];

            if (divergences == null || !divergences.Any())
                return 0;
            return divergences.Count();
        }

        public string DistanceMatrixLog(bool useMedianTimes = false, bool useOnlyConcordant = true, bool timedHAL = true)
        {
            List<int> OrderedIndex = RepresentativeDictionary.Keys.OrderBy(x => x).ToList();

            StringBuilder log = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            foreach (int i in OrderedIndex)
            {

                sb.Append(String.Format("{0,3}", i) + "\t");
                sb2.Append(String.Format("{0,3}", i) + "\t");                    
                log.AppendLine(String.Format("{0,3}", i) + " " + String.Join(",", RepresentativeDictionary[i].getLeaves(false).Select(x => x.TaxonName)));

                foreach (int j in OrderedIndex)
                {
                    if (this[i, j] != null && this[i, j].Count() > 0)
                    {
                        double count = supportingStudies(i, j);
                        double total = totalStudies(i, j);
                        double percentSupport = count / total;

                        int agreement = countAgreement(i, j);

                        if (!timedHAL)
                        {
                            if (useMedianTimes)
                                sb.Append(String.Format("{0:0.00}", this[i, j].Select(x => x.Value.DivergenceTime).Median()) + "\\" + String.Format("{0:0.00}", percentSupport) + "\t");
                            else
                                sb.Append(String.Format("{0:0.00}", this[i, j].Select(x => x.Value.DivergenceTime).Average()) + "\\" + String.Format("{0:0.00}", percentSupport) + "\t");
                            sb2.Append(String.Format("{0,4}", count) + "\\" + String.Format("{0,-4}", total));
                        }
                        else
                        {
                            bool topologyConflict = HasConcordantDivergences(i, j);

                            sb.Append(String.Format("{0:0.00}", distance(i, j, useMedian: useMedianTimes, useOnlyConcordant: useOnlyConcordant)) + "\\" + topologyConflict + "\t");
                            sb2.Append(String.Format("{0,4}", agreement) + "\\" + String.Format("{0,-4}", count));
                        }
                    }
                    else
                    {
                        sb.Append("DNaN\\DNaN ");
                        sb2.Append("    0\\0    ");
                    }
                }

                sb.AppendLine();
                sb2.AppendLine();
            }
            log.AppendLine();
            log.AppendLine(sb.ToString() + "\t" + String.Join("\t", OrderedIndex.Select(x => String.Format("{0,5}", x))));

            log.AppendLine();
            log.AppendLine(sb2.ToString() + "\t" + String.Join("\t", OrderedIndex.Select(x => String.Format("{0,5}", x))));

            return log.ToString();
        }
    }



    public class HALFunctions
    {
        private Dictionary<Tuple<TopoTimeNode, TopoTimeNode>, Tuple<Dictionary<int, Divergence>, int, string, string>> cachedDivergences;
        public PartitionFetcher partitionFetcher;
        private TopoTimeTree activeTree;
        private DatabaseService DBService;

        public HALFunctions(DatabaseService DBService, TopoTimeTree activeTree)
        {
            cachedDivergences = new Dictionary<Tuple<TopoTimeNode, TopoTimeNode>, Tuple<Dictionary<int, Divergence>, int, string, string>>();
            partitionFetcher = new PartitionFetcher(DBService);
            this.DBService = DBService;
            this.activeTree = activeTree;

            this.activeTree.includedStudies = new SerializableDictionary<int, Study>(partitionFetcher.CitationData);
        }

        public UPGMAMatrix FastHALX(TopoTimeNode target, PartitionFetcher fetcher, StringBuilder log, bool useFloatingNodes = true, bool skipNestedNodes = false, bool debug = false)
        {
            Dictionary<int, TopoTimeNode> FloatingNodes = new Dictionary<int, TopoTimeNode>();

            if (useFloatingNodes)
            {
                TopoTimeNode currentParent = target.Parent;
                while (currentParent != null)
                {
                    if (currentParent.storedFloatingNodes != null)
                    {
                        foreach (var pair in currentParent.storedFloatingNodes)
                        {
                            FloatingNodes[pair.Value] = pair.Key;
                        }
                    }

                    currentParent = currentParent.Parent;
                }                
            }

            IEnumerable<PartitionData> partitionList = fetcher.GetTimeRelevantPartitions(target, FloatingNodes, skipNestedNodes: skipNestedNodes);            

            if (!partitionList.Any())
                return null;

            int divergenceCount = 0;
            int issueCount = 0;

            UPGMAMatrix distMatrix = new UPGMAMatrix();

            HashSet<(int, int)> ignoreList = new HashSet<(int, int)>(partitionList.Count());

            foreach (PartitionData partition in partitionList)
            {
                List<int> nodesA = partition.nodesA.Distinct().ToList();
                List<int> nodesB = partition.nodesB.Distinct().ToList();

                foreach (int i in nodesA)
                {
                    if (ignoreList.Contains((partition.PhylogenyNodeIDs[0], i)))
                    {
                        ignoreList.Add((partition.childA_phylogeny_node, i));
                        ignoreList.Add((partition.childB_phylogeny_node, i));
                        continue;
                    }

                    // ignore floating taxa if they've already been added
                    // TO-DO: ideally figure out a way to filter these BEFORE they're sent to the SQL function
                    /*
                    if (i < 0 && FloatingNodes[i].Parent == target)
                    {
                        //ignoreList.Add((partition.childA_phylogeny_node, i));
                        //ignoreList.Add((partition.childB_phylogeny_node, i));
                        continue;
                    }
                    */

                    if (nodesB.Contains(i))
                    {
                        ignoreList.Add((partition.childA_phylogeny_node, i));
                        ignoreList.Add((partition.childB_phylogeny_node, i));
                        continue;
                    }

                    bool containsOutgroup = (nodesA.Count > 1 || nodesB.Count > 1);

                    // if node has index < 0, it's a floating node (unclassified, incertae sedis)
                    TopoTimeNode representativeA;
                    if (i < 0)
                        representativeA = FloatingNodes[i];
                    else
                        representativeA = target.Nodes[i];

                    foreach (int j in nodesB)
                    {
                        // ignore floating taxa if they've already been added
                        // TO-DO: ideally figure out a way to filter these BEFORE they're sent to the SQL function
                        /*
                        if (j < 0 && FloatingNodes[j].Parent == target)
                        {
                            //ignoreList.Add((partition.childA_phylogeny_node, j));
                            //ignoreList.Add((partition.childB_phylogeny_node, j));
                            continue;
                        }
                        */

                        TopoTimeNode representativeB;
                        if (j < 0)
                            representativeB = FloatingNodes[j];
                        else
                            representativeB = target.Nodes[j];

                        /*
                        // at least one of the prospective nodes should be attached to the current node
                        if (representativeA.Parent != target && representativeB != target)
                        {
                            //ignoreList.Add((partition.childA_phylogeny_node, i));
                            //ignoreList.Add((partition.childB_phylogeny_node, j));
                            continue;
                        }
                        */

                        if (i != j)
                        {
                            distMatrix.AddDivergence(i, j, representativeA, representativeB, partition.DivergenceTime, partition.citation, partition.PhylogenyNodeIDs[0], containsOutgroup);
                            divergenceCount++;
                        }
                        else
                            issueCount++;

                    }
                }
            }

            return distMatrix;
        }

        public IEnumerable<TopoTimeNode> ResolveFastHALX(TopoTimeNode target, PartitionFetcher fetcher, StringBuilder log = null, bool timedHAL = true, bool useOnlyConcordant = true, bool skipNestedNodes = false, double supportThreshold = 0.5, int GuideTreeID = 0)
        {
            if (target.Nodes.Count < 3)
                yield break;
            
            UPGMAMatrix distMatrix = FastHALX(target, fetcher, log);

            if (distMatrix == null)
                yield break;

            //if (log != null)
            //    log.Append(distMatrix.DistanceMatrixLog(activeTree.UseMedianTimes, useOnlyConcordant, timedHAL));

            double maxs = Double.NegativeInfinity;
            double mind = Double.PositiveInfinity;

            bool TopologyConflict = false;

            // experimental: prevents resolutions being made that are younger than the oldest existing resolution
            // however, this tends to leave trees unresolved, so I've since left it disabled

            double hardMinimumTime = double.NegativeInfinity;

            /*
            foreach (ExtendedNode child in target.Nodes)
            {
                if (child.PreAdjustedHeight != 0 && child.PreAdjustedHeight > hardMinimumTime)
                    hardMinimumTime = child.PreAdjustedHeight;
            }
            */

            List<(int, int, double)> BestPairs = new List<(int, int, double)>();

            foreach (var pair in distMatrix.PairDivergenceDictionary)
            {
                (int i, int j) = pair.Key;

                if (timedHAL)
                {
                    double d = distMatrix.distance(i, j, useMedian: activeTree.UseMedianTimes, useOnlyConcordant: useOnlyConcordant, guideTree: GuideTreeID);
                    double support = distMatrix.percentSupport(i, j, guideTree: GuideTreeID);

                    bool topologyConflict;

                    if (useOnlyConcordant)
                        topologyConflict = false;
                    else
                        topologyConflict = distMatrix.TopologyConflict(i, j);

                    if (support > supportThreshold)
                    {                        
                        if (d > hardMinimumTime && d < mind && d > 0)
                        {
                            BestPairs.Clear();

                            mind = d;
                            maxs = support;
                            TopologyConflict = topologyConflict;

                            /*
                            if (d < altd)
                            {
                                altd = d;
                                alti = i;
                                altj = j;
                                alts = support;
                            }
                            */
                        }
                        else if (d == 0 && !topologyConflict)
                        {
                            if (mind > 0)
                            {
                                mind = 0;
                                BestPairs.Clear();
                            }
                            maxs = support;
                            TopologyConflict = topologyConflict;
                        }
                        /*
                        else if (Double.IsNaN(d) && Double.IsPositiveInfinity(mind))
                        {

                            if (!topologyConflict)
                            {
                                maxi = i;
                                maxj = j;
                                maxs = support;
                                TopologyConflict = topologyConflict;
                            }
                        }
                        */

                        if (d == mind && d >= 0)
                            BestPairs.Add((i, j, support));
                    }
                    /*
                    else
                    {
                        if (d > hardMinimumTime && d > 0)
                        {
                            if (support >= alts)
                            {
                                if (support > alts)
                                {
                                    altd = d;
                                    alti = i;
                                    altj = j;
                                    alts = support;
                                    TopologyConflict = topologyConflict;
                                }
                                else if (d < altd)
                                {
                                    altd = d;
                                    alti = i;
                                    altj = j;
                                    alts = support;
                                    TopologyConflict = topologyConflict;
                                }
                            }
                        }
                        else if (d <= 0 && support > alts && !topologyConflict)
                        {
                            //mind = d;
                            alti = i;
                            altj = j;
                            alts = support;
                            TopologyConflict = topologyConflict;
                        }
                        else if (Double.IsNaN(d) && Double.IsPositiveInfinity(altd))
                        {

                            if (!topologyConflict)
                            {
                                alti = i;
                                altj = j;
                                alts = support;
                                TopologyConflict = topologyConflict;
                            }
                        }
                    }
                    */

                }
                else
                {

                    double d = distMatrix.distance(i, j, useMedian: activeTree.UseMedianTimes, useOnlyConcordant: useOnlyConcordant);
                    double support = Math.Round(distMatrix.percentSupport(i, j), 2, MidpointRounding.AwayFromZero);

                    if (support == maxs && support > 0)
                        BestPairs.Add((i, j, support));



                    if (support > maxs && support != 0)
                    {
                        BestPairs.Clear();
                        BestPairs.Add((i, j, support));

                        maxs = support;                        
                        mind = d;
                    }

                    if (support == maxs)
                    {
                        if (d < mind || mind <= 0)
                        {                           

                            BestPairs.Add((i, j, support));
                            mind = d;
                        }

                    }
                }
            }

            foreach (var pair in BestPairs)
            {
                (int besti, int bestj, double bests) = pair;

                TopoTimeNode newParent = new TopoTimeNode();
                TopoTimeNode firstChild = distMatrix[(int)besti];
                TopoTimeNode secondChild = distMatrix[(int)bestj];

                // temporarily disabled floating taxa handling
                /*
                if (besti < 0)
                    activeTree.FloatingTaxa.Remove((int)besti);

                if (bestj < 0)
                    activeTree.FloatingTaxa.Remove((int)bestj);
                    */

                TopoTimeNode targetParent = target;

                /*
                if (firstChild.Parent == secondChild.Parent && firstChild.Parent != target)
                {
                    targetParent = firstChild.Parent;
                    activeTree.FloatingTaxa.Add(activeTree.FloatingTaxa.Keys.Min() - 1, targetParent);
                }
                */

                activeTree.AddNode(newParent, targetParent);

                activeTree.MoveNode2(firstChild, newParent);
                activeTree.MoveNode2(secondChild, newParent);

                if (firstChild.Floating)
                    firstChild.Floating = false;

                if (secondChild.Floating)
                    secondChild.Floating = false;

                newParent.TaxonName = "";
                List<Tuple<double, ChildPairDivergence>> divergenceList = getDivergenceCitations(distMatrix.ValidDivergences((int)besti, (int)bestj), newParent, DBService);

                if (divergenceList != null)
                {
                    foreach (Tuple<double, ChildPairDivergence> divergence in divergenceList)
                    {
                        if (divergence.Item2.DivergenceTime > 0)
                            newParent.ChildDivergences.Add(divergence.Item2);
                    }
                }

                newParent.TotalStudies = distMatrix.totalStudies((int)besti, (int)bestj);
                newParent.SupportingStudies = distMatrix.supportingStudies((int)besti, (int)bestj);
                newParent.percentSupport = bests;

                
                yield return newParent;
            }
        }

        public List<TopoTimeNode> ResolveUsingConsensus(TopoTimeNode target, PartitionFetcher fetcher, StringBuilder log = null, bool timedHAL = true, bool useOnlyConcordant = true, bool skipNestedNodes = false)
        {
            if (target.Nodes.Count < 3)
                return null;

            List<PartitionData> partitionList = fetcher.GetTimeRelevantPartitions(target, null, skipNestedNodes: skipNestedNodes).ToList();

            Dictionary<Tuple<int, int>, int> IndividualPairs = new Dictionary<Tuple<int, int>, int>();

            foreach (PartitionData partition in partitionList)
            {
                foreach (int nodeA in partition.nodesA)
                {
                    foreach (int nodeB in partition.nodesB)
                    {
                        int min = Math.Min(nodeA, nodeB);
                        int max = Math.Max(nodeA, nodeB);

                        Tuple<int, int> pair = new Tuple<int, int>(min, max);

                        if (!IndividualPairs.ContainsKey(pair))
                            IndividualPairs[pair] = 0;

                        IndividualPairs[pair]++;
                    }
                }
            }

            int studyCount = partitionList.Select(x => x.citation).Distinct().Count();
            TopoTimeNode[] representatives = target.Nodes.Cast<TopoTimeNode>().ToArray();
            List<TopoTimeNode> NewNodes = new List<TopoTimeNode>();

            foreach (var pair in IndividualPairs.Keys)
            {
                if (IndividualPairs[pair] > studyCount / 2)
                {
                    TopoTimeNode newParent = new TopoTimeNode();

                    TopoTimeNode nodeA = representatives[pair.Item1];
                    TopoTimeNode nodeB = representatives[pair.Item2];

                    TopoTimeNode parentA = nodeA.Parent;
                    TopoTimeNode parentB = nodeB.Parent;

                    parentA.Nodes.Remove(nodeA);
                    parentB.Nodes.Remove(nodeB);

                    newParent.Nodes.Add(nodeA);
                    newParent.Nodes.Add(nodeB);

                    NewNodes.Add(newParent);
                    target.Nodes.Add(newParent);

                    newParent.Expand();
                }
                
            }

            return NewNodes;
        }

        public Tuple<TopoTimeNode, TopoTimeNode> ResolveFastHAlXConsensus(TopoTimeNode target, PartitionFetcher fetcher, StringBuilder log = null, bool timedHAL = true, bool useOnlyConcordant = true, bool skipNestedNodes = false)
        {
            if (target.Nodes.Count < 3)
                return null;

            List<PartitionData> partitionList = fetcher.GetTimeRelevantPartitions(target, skipNestedNodes: skipNestedNodes).ToList();

            var partitionGrouping = from partition in partitionList
                                    group partition by partition.citation into g
                                    select g.OrderByDescending(x => x.phylogeny_node_id).First();

            var consensusList = ListAllPartitionGroups(partitionGrouping).ToList();
            var consensusGrouping = consensusList.GroupBy(x => x, new ListGrouper());

            int StudyCount = partitionGrouping.Count();

            foreach (var group in consensusGrouping)
            {
                if (group.Count() == StudyCount)
                {
                    List<TopoTimeNode> representatives = group.Key.Select(x => target.Nodes[x]).ToList();
                    List<TopoTimeNode> outgroup = target.Nodes.Cast<TopoTimeNode>().Except(representatives).ToList();

                    TopoTimeNode firstGroup;
                    TopoTimeNode secondGroup;

                    if (representatives.Count == 1)
                    {
                        firstGroup = representatives[0];
                    }
                    else
                    {
                        firstGroup = new TopoTimeNode();
                        foreach (TopoTimeNode node in representatives)
                        {
                            activeTree.MoveNode2(node, firstGroup);
                        }

                        activeTree.AddNode(firstGroup, target);
                        firstGroup.TaxonName = "";

                        activeTree.AddNewNodeToTree(firstGroup);
                        firstGroup.Expand();
                    }

                    if (outgroup.Count == 1)
                    {
                        secondGroup = outgroup[0];
                    }
                    else
                    {
                        secondGroup = new TopoTimeNode();
                        foreach (TopoTimeNode node in outgroup)
                        {
                            activeTree.MoveNode2(node, secondGroup);
                        }

                        activeTree.AddNode(secondGroup, target);
                        secondGroup.TaxonName = "";

                        activeTree.AddNewNodeToTree(secondGroup);
                        secondGroup.Expand();
                    }

                    target.TotalStudies = StudyCount;
                    target.SupportingStudies = StudyCount;
                    target.percentSupport = 1;

                    activeTree.UpdateNodeText(target);

                    return new Tuple<TopoTimeNode, TopoTimeNode>(firstGroup, secondGroup);
                }
            }

            return null;
        }

        public IEnumerable<List<int>> ListAllPartitionGroups(IEnumerable<PartitionData> partitionList)
        {
            foreach (PartitionData partition in partitionList)
            {
                yield return partition.nodesA;
                yield return partition.nodesB;
            }
        }

        public IEnumerable<TopoTimeNode> ResolveMultipleHAL(TopoTimeNode target, PartitionFetcher FastHALFetcher = null, bool UseOnlyConcordant = true, double SupportThreshold = 0.50, bool SkipNestedNodes = false, int GuideTreeID = 0)
        {
            if (target.Nodes.Count < 3)
                yield break;

            yield return null;
        }

        public enum TopologyConflictMode
        {
            NoConflicts,
            UseConflictsIfNecessary,
            UseConflictsAnywhere
        }

        public TopoTimeNode ResolveHAL(TopoTimeNode target, UPGMANode[] distMatrix, int K, StringBuilder log, bool timedHAL = true, PartitionFetcher fastHALfetcher = null, TopologyConflictMode UseConflicts = TopologyConflictMode.NoConflicts, double supportThreshold = 0.50, bool skipNestedNodes = false, int guideTree = 0)
        {
            if (target.Nodes.Count < 3)
                return null;

           
            TopoTimeNode newParent = new TopoTimeNode();

            UPGMANode[] tempNodes;

            bool fastHAL = fastHALfetcher != null;

            bool debug = log != null;

            if (distMatrix == null)
            {
                if (!fastHAL)
                    tempNodes = OriginalHAL(target, DBService, log);
                else
                    tempNodes = FastHAL(target, fastHALfetcher, log, debug: debug, skipNestedNodes: skipNestedNodes);
            }
            else
                tempNodes = distMatrix;

            if (tempNodes == null)
                return null;

            if (log != null)
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                for (int i = 0; i < K; i++)
                {
                    sb.Append(String.Format("{0,3}", i) + "\t");
                    sb2.Append(String.Format("{0,3}", i) + "\t");
                    if (tempNodes[i] != null)
                    {
                        if (log != null)
                        {
                            log.AppendLine(String.Format("{0,3}", i) + " " + String.Join(",", tempNodes[i].representative.getLeaves(false).Select(x => x.TaxonName)));
                        }

                        for (int j = 0; j < tempNodes[i].distances.Length; j++)
                        {
                            if (tempNodes[i].distances[j] != null && tempNodes[i].distances[j].Count() > 0)
                            {
                                double count = UPGMANode.supportingStudies(i, j, tempNodes);
                                double total = UPGMANode.totalStudies(i, j, tempNodes);
                                double percentSupport = count / total;

                                int agreement = UPGMANode.countAgreement(i, j, tempNodes);

                                if (!timedHAL)
                                {

                                    if (tempNodes[j] != null)
                                    {
                                        if (activeTree.UseMedianTimes)
                                        sb.Append(String.Format("{0:0.00}", tempNodes[i].distances[j].Select(x => x.Value.DivergenceTime).Median()) + "\\" + String.Format("{0:0.00}", percentSupport) + "\t");
                                        else
                                            sb.Append(String.Format("{0:0.00}", tempNodes[i].distances[j].Select(x => x.Value.DivergenceTime).Median()) + "\\" + String.Format("{0:0.00}", percentSupport) + "\t");
                                        sb2.Append(String.Format("{0,4}", count) + "\\" + String.Format("{0,-4}", total));
                                    }
                                    else
                                    {
                                        sb.Append("NODATA");
                                        sb2.Append("NODATA");
                                    }
                                }
                                else
                                {
                                    bool topologyConflict = UPGMANode.HasConcordantDivergences(i, j, tempNodes);

                                    if (tempNodes[j] != null)
                                    {
                                        sb.Append(String.Format("{0:0.00}", UPGMANode.distance(i, j, tempNodes, useMedian: activeTree.UseMedianTimes, UseConflicts: UseConflicts)) + "\\" + topologyConflict + "\t");
                                        sb2.Append(String.Format("{0,4}", agreement) + "\\" + String.Format("{0,-4}", count));
                                    }
                                    else
                                    {
                                        sb.Append("NODATA");
                                        sb2.Append("NODATA");
                                    }
                                }
                            }
                            else
                            {
                                sb.Append("DNaN\\DNaN\t");
                                sb2.Append("0\\0\t");
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0; j < i; j++)
                        {
                            //sb.Append("DNaN\\DNaN ");
                        }
                    }
                    sb.AppendLine();
                    sb2.AppendLine();
                }
                log.AppendLine();
                log.AppendLine(sb.ToString() + "\t" + String.Join("\t", Enumerable.Range(0, K).Select(x => String.Format("{0,5}", x))));

                log.AppendLine();
                log.AppendLine(sb2.ToString() + "\t" + String.Join("\t", Enumerable.Range(0, K).Select(x => String.Format("{0,5}", x))));
            }          

            

            int maxi = -1;
            int maxj = -1;

            int alti = -1;
            int altj = -1;

            double maxs = Double.NegativeInfinity;
            double alts = Double.NegativeInfinity;
            double mind = Double.PositiveInfinity;
            double altd = Double.PositiveInfinity;

            bool TopologyConflict = false;

            // experimental: prevents resolutions being made that are younger than the oldest existing resolution
            // however, this tends to leave trees unresolved, so I leave it disabled usually

            double hardMinimumTime = double.NegativeInfinity;

            /*
            foreach (ExtendedNode child in target.Nodes)
            {
                if (child.PreAdjustedHeight != 0 && child.PreAdjustedHeight > hardMinimumTime)
                    hardMinimumTime = child.PreAdjustedHeight;
            }
            */
            

            for (int i = 0; i < K; i++)
            {
                if (tempNodes[i] != null)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (tempNodes[j] != null)
                        {
                            if (timedHAL)
                            {
                                double d = UPGMANode.distance(i, j, tempNodes, useMedian: activeTree.UseMedianTimes, UseConflicts: UseConflicts, guideTree: guideTree);
                                double support = UPGMANode.percentSupport(i, j, tempNodes, guideTree: guideTree);

                                bool topologyConflict;

                                if (UseConflicts == TopologyConflictMode.NoConflicts)
                                    topologyConflict = false;
                                else 
                                    topologyConflict = UPGMANode.TopologyConflict(i, j, tempNodes);

                                if (support > supportThreshold)
                                {
                                    if (d > hardMinimumTime && d < mind && d > 0)
                                    {
                                        mind = d;
                                        maxi = i;
                                        maxj = j;
                                        maxs = support;
                                        TopologyConflict = topologyConflict;

                                        if (d < altd)
                                        {
                                            altd = d;
                                            alti = i;
                                            altj = j;
                                            alts = support;
                                        }
                                    }
                                    else if (d <= 0 && !topologyConflict)
                                    {
                                        if (guideTree > 0)
                                            mind = d;

                                        maxi = i;
                                        maxj = j;
                                        maxs = support;
                                        TopologyConflict = topologyConflict;
                                    }
                                    else if (Double.IsNaN(d) && Double.IsPositiveInfinity(mind))
                                    {

                                        if (!topologyConflict)
                                        {
                                            maxi = i;
                                            maxj = j;
                                            maxs = support;
                                            TopologyConflict = topologyConflict;
                                        }
                                    }
                                }
                                else
                                {
                                    if (d > hardMinimumTime && d > 0)
                                    {
                                        if (support >= alts)
                                        {
                                            if (support > alts)
                                            {
                                                altd = d;
                                                alti = i;
                                                altj = j;
                                                alts = support;
                                                TopologyConflict = topologyConflict;
                                            }
                                            else if (d < altd)
                                            {
                                                altd = d;
                                                alti = i;
                                                altj = j;
                                                alts = support;
                                                TopologyConflict = topologyConflict;
                                            }
                                        }                                        
                                    }
                                    else if (d <= 0 && support > alts && !topologyConflict)
                                    {
                                        //mind = d;
                                        alti = i;
                                        altj = j;
                                        alts = support;
                                        TopologyConflict = topologyConflict;
                                    }
                                    else if (Double.IsNaN(d) && Double.IsPositiveInfinity(altd))
                                    {

                                        if (!topologyConflict)
                                        {
                                            alti = i;
                                            altj = j;
                                            alts = support;
                                            TopologyConflict = topologyConflict;
                                        }
                                    }
                                }
                                
                            }
                            else
                            {

                                double d = UPGMANode.distance(i, j, tempNodes, useMedian: activeTree.UseMedianTimes, UseConflicts: UseConflicts);
                                double support = Math.Round(UPGMANode.percentSupport(i, j, tempNodes), 2, MidpointRounding.AwayFromZero);

                                if (support > maxs && support != 0)
                                {
                                    maxs = support;
                                    maxi = i;
                                    maxj = j;

                                    mind = d;
                                }

                                if (support == maxs)
                                {
                                    if (d < mind || mind <= 0)
                                    {
                                        mind = d;

                                        maxi = i;
                                        maxj = j;
                                    }

                                }
                            }
                        }
                    }
                }
            }

            int besti = maxi;
            int bestj = maxj;
            double bests = maxs;

            if (besti == -1 && UseConflicts != TopologyConflictMode.NoConflicts)
            //if (besti == -1 && (alts > 0 || !useOnlyConcordant))
            {
                besti = alti;
                bestj = altj;
                bests = alts;
            }

            tempNodes[K] = new UPGMANode(K);
            tempNodes[K].representative = newParent;

            

            if (besti > -1 && bestj > -1)
            {

                TopoTimeNode firstChild = tempNodes[besti].representative;
                TopoTimeNode secondChild = tempNodes[bestj].representative;

                //tempNodes[besti].lists_a[bestj] = new List<string>() { "test1", "test2" };
                //tempNodes[besti].lists_b[bestj] = new List<string>() { "test1", "test2" };

                target.Nodes.Remove(firstChild);
                target.Nodes.Remove(secondChild);

                newParent.Nodes.Add(firstChild);
                newParent.Nodes.Add(secondChild);

                newParent.TaxonName = "";

                /*
                leavesA = newParent.getNamedChildren();
                //leavesA.RemoveAll(x => x.TaxaID == -1);

                
                for (int m = 0; m < K; m++)
                {
                    if (tempNodes[m] != null && m != besti && m != bestj)
                    {
                        leavesB = tempNodes[m].representative.getNamedChildren();
                        //leavesB.RemoveAll(x => x.TaxaID == -1);
                        leavesC = target.getNamedChildren().Except(leavesA.Union(leavesB)).ToList();

                        int contradict = 0;
                        string list_a = "";
                        string list_b = "";
                        Dictionary<int, Divergence> divergences = getDivergenceTimesOnly(leavesA, leavesB, leavesC, out contradict, ref list_a, ref list_b, conn, !oldHAL);

                        tempNodes[K].AddDivergence(m, divergences, contradict, list_a, list_b);
                    }
                }
                */

                //List<Tuple<double, ChildPairDivergence>> divergenceList = getDivergenceCitations(UPGMANode.ValidDivergences(besti, bestj, tempNodes), tempNodes[besti].lists_a[bestj], tempNodes[besti].lists_b[bestj], conn);
                List<Tuple<double, ChildPairDivergence>> divergenceList = getDivergenceCitations(UPGMANode.ValidDivergences(besti, bestj, tempNodes), newParent, DBService);

                if (divergenceList != null)
                {
                    foreach (Tuple<double, ChildPairDivergence> divergence in divergenceList)
                    {
                        //if (divergence.Item2.DivergenceTime > 0)
                            newParent.ChildDivergences.Add(divergence.Item2);
                    }
                }

                newParent.TotalStudies = UPGMANode.totalStudies(besti, bestj, tempNodes);
                newParent.SupportingStudies = UPGMANode.supportingStudies(besti, bestj, tempNodes);
                newParent.percentSupport = bests;

                tempNodes[besti] = null;
                tempNodes[bestj] = null;
                

                activeTree.AddNode(newParent, target);
                return newParent;
            }
            else
                return null;

        }

        public UPGMANode[] FastHAL(TopoTimeNode target, PartitionFetcher fetcher, StringBuilder log, bool debug = false, bool skipNestedNodes = false)
        {
            List<PartitionData> partitionList = fetcher.GetTimeRelevantPartitions(target, skipNestedNodes: skipNestedNodes).ToList();

            if (!partitionList.Any())
                return null;

            int N = target.Nodes.Count;
            int divergenceCount = 0;
            int issueCount = 0;

            //UPGMANode[] tempNodes = new UPGMANode[N * 2 - 1];

            // presently we are not reusing distance matrices so we can just generate a smaller one
            UPGMANode[] tempNodes = new UPGMANode[N + 1];
            for (int x = 0; x < N; x++)
            {
                tempNodes[x] = new UPGMANode(x);
                tempNodes[x].representative = (TopoTimeNode)target.Nodes[x];
            }

            HashSet<(int, int)> ignoreList = new HashSet<(int, int)>();

            foreach (PartitionData partition in partitionList)
            {
                List<int> nodesA = partition.nodesA.Distinct().ToList();
                List<int> nodesB = partition.nodesB.Distinct().ToList();

                foreach (int i in nodesA)
                {
                    if (ignoreList.Contains((partition.PhylogenyNodeIDs[0], i)))
                    {
                        ignoreList.Add((partition.childA_phylogeny_node, i));
                        ignoreList.Add((partition.childB_phylogeny_node, i));
                        continue;
                    }

                    if (nodesB.Contains(i))
                    {
                        ignoreList.Add((partition.childA_phylogeny_node, i));
                        ignoreList.Add((partition.childB_phylogeny_node, i));
                        continue;
                    }

                    bool containsOutgroup = (nodesA.Count > 1 || nodesB.Count > 1);

                    //Study citationData = fetcher.CitationData[partition.citation];
                    //string refID = citationData.RefID;
                    //string author = citationData.Author;
                    //string title = citationData.Title;
                    //int year = citationData.Year;

                    foreach (int j in nodesB)
                    {
                        if (i > j)
                        {
                            tempNodes[i].AddDivergence(j, partition.DivergenceTime, partition.citation, partition.PhylogenyNodeIDs[0], containsOutgroup);
                            divergenceCount++;
                        }
                        else if (j > i)
                        {
                            tempNodes[j].AddDivergence(i, partition.DivergenceTime, partition.citation, partition.PhylogenyNodeIDs[0], containsOutgroup);
                            divergenceCount++;
                        }
                        else
                            issueCount++;

                    }
                }
            }

            if (debug)
            {
                int matrixCount = CountDivergencesInMatrix(tempNodes);
                if (matrixCount != divergenceCount)
                    throw new Exception("Distance matrix count mismatch");
            }


            return tempNodes;
        }

        public int CountDivergencesInMatrix(UPGMANode[] distMatrix)
        {
            int count = 0;
            for (int i = 0; i < distMatrix.Length - 1; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (distMatrix[i].distances[j] != null)
                    {
                        foreach (Divergence div in distMatrix[i].distances[j].Values)
                            count += div.DivergenceTimes.Count;                        
                    }
                }
            }
            return count;
        }

        public UPGMANode[] OriginalHAL(TopoTimeNode target, DatabaseService DBService, StringBuilder log)
        {
            IEnumerable<TopoTimeNode> leavesA;
            IEnumerable<TopoTimeNode> leavesB;
            IEnumerable<TopoTimeNode> leavesC = null;

            int N = target.Nodes.Count;

            UPGMANode[] tempNodes = new UPGMANode[N * 2 - 1];

            for (int i = 0; i < N; i++)
            {
                UPGMANode newNode = new UPGMANode(i);
                newNode.representative = (TopoTimeNode)target.Nodes[i];

                leavesA = target.Nodes[i].getNamedChildren();
                //leavesA.RemoveAll(x => x.TaxaID == -1);

                if (log != null)
                {
                    //log.AppendLine(String.Format("{0,3}", i) + " " + String.Join(",", leavesA.Select(x => x.TaxaName)));
                }

                for (int j = 0; j < i; j++)
                {
                    if (j == i)
                        continue;

                    Dictionary<int, Divergence> divergences;
                    int contradict;

                    string list_a = "";
                    string list_b = "";

                    Tuple<TopoTimeNode, TopoTimeNode> pair = new Tuple<TopoTimeNode, TopoTimeNode>((TopoTimeNode)target.Nodes[i], (TopoTimeNode)target.Nodes[j]);

                    if (cachedDivergences.ContainsKey(pair))
                    {
                        divergences = cachedDivergences[pair].Item1;
                        contradict = cachedDivergences[pair].Item2;
                        list_a = cachedDivergences[pair].Item3;
                        list_b = cachedDivergences[pair].Item4;
                    }
                    else
                    {
                        leavesB = target.Nodes[j].getNamedChildren();
                        //leavesB.RemoveAll(x => x.TaxaID == -1);
                        //if (!oldHAL)
                        leavesC = target.getNamedChildren().Except(leavesA.Union(leavesB)).ToList();
                        //leavesC = null;


                        divergences = getDivergenceTimesOnly(leavesA, leavesB, leavesC, out contradict, ref list_a, ref list_b, DBService, retainEmptyTimes: true, retainConflicts: true);
                        cachedDivergences.Add(pair, new Tuple<Dictionary<int, Divergence>, int, string, string>(divergences, contradict, list_a, list_b));
                    }

                    //newNode.AddDivergence(j, divergences, contradict, list_a, list_b);
                    // placeholder because we probably won't go back to Original HAL
                    newNode.AddDivergence(j, divergences, contradict);
                }

                tempNodes[i] = newNode;
            }

            return tempNodes;
        }

        public Dictionary<int, double> CoverageAnalysis(TopoTimeNode target, StringBuilder log)
        {
            List<PartitionData> partitionList = partitionFetcher.GetTimeRelevantPartitions(target).ToList();

            int nodeCount = target.Nodes.Count;

            Dictionary<int, HashSet<int>> CoverageList = new Dictionary<int, HashSet<int>>();

            foreach (PartitionData partition in partitionList)
            {
                if (!CoverageList.ContainsKey(partition.citation))
                    CoverageList[partition.citation] = new HashSet<int>();

                foreach (int nodeIndex in partition.nodesA)
                    CoverageList[partition.citation].Add(nodeIndex);

                foreach (int nodeIndex in partition.nodesB)
                    CoverageList[partition.citation].Add(nodeIndex);
            }

            if (log != null)
            {
                foreach (KeyValuePair<int, HashSet<int>> pair in CoverageList)
                {
                    int CitationID = pair.Key;
                    HashSet<int> coverage = pair.Value;

                    double percentCoverage = (double)coverage.Count / (double)nodeCount * 100;

                    log.AppendLine($"{CitationID}: {coverage.Count} ({percentCoverage}%)");
                }
            }

            return CoverageList.ToDictionary(x => x.Key, y => (double)y.Value.Count / (double)nodeCount);
        }

        public void RootToTipResolution(TopoTimeNode parentNode, StringBuilder log = null, bool timedHAL = true, PartitionFetcher fastHALfetcher = null, TopologyConflictMode UseConflicts = TopologyConflictMode.NoConflicts)
        {
            TopoTimeNode resolvedRoot = ResolveHAL(parentNode, null, parentNode.Nodes.Count, log, timedHAL: timedHAL, fastHALfetcher: fastHALfetcher, UseConflicts: UseConflicts);

            //ExtendedNode resolvedRoot = ResolveFastHAL(parentNode, log, fastHALfetcher, timedHAL: timedHAL, useOnlyConcordant: useOnlyConcordant);

            if (resolvedRoot != null)
            {
                TopoTimeNode newNode = resolvedRoot;
                newNode.Expand();

                activeTree.AddNewNodeToTree(newNode);
            }

            setTimesHAL(parentNode);
        }

 
        /*
        public void setTimesHAL(ExtendedNode target, StringBuilder log = null)
        {
            if (target.Nodes.Count != 2)
                return;

            UPGMAMatrix distMatrix = FastHALX(target, partitionFetcher, log, activeTree.FloatingTaxa);

            int besti = 0;
            int bestj = 1;

            

            if (distMatrix != null)
            {
                IEnumerable<KeyValuePair<int, Divergence>> validDivergences = distMatrix.ValidDivergences(besti, bestj);

                List<Tuple<double, ChildPairDivergence>> divergenceList = getDivergenceCitations(validDivergences, target, conn);

                if (divergenceList != null)
                {
                    target.ChildDivergences.Clear();
                    foreach (Tuple<double, ChildPairDivergence> divergence in divergenceList)
                    {
                        if (divergence.Item2.DivergenceTime > 0)
                            target.ChildDivergences.Add(divergence.Item2);
                    }
                }

                target.TotalStudies = distMatrix.totalStudies(besti, bestj);
                target.SupportingStudies = distMatrix.supportingStudies(besti, bestj);
                target.percentSupport = (double)target.SupportingStudies / (double)target.TotalStudies;
            }            

            target.UpdateText();
        }
        */

        
        public void setTimesHAL(TopoTimeNode target, StringBuilder log = null, TopologyConflictMode UseConflicts = TopologyConflictMode.NoConflicts)
        {
            if (target.Nodes.Count != 2)
                return;

            UPGMANode[] tempNodes = FastHAL(target, partitionFetcher, log);

            int besti = 0;
            int bestj = 1;

            bool nodeUpdated = target.ChildDivergences.Count > 0;

            if (tempNodes != null)
            {
                IEnumerable<KeyValuePair<int, Divergence>> validDivergences = UPGMANode.ValidDivergences(besti, bestj, tempNodes);

                List<Tuple<double, ChildPairDivergence>> divergenceList = getDivergenceCitations(validDivergences, target, DBService);

                if (divergenceList != null)
                {
                    target.ChildDivergences.Clear();
                    foreach (Tuple<double, ChildPairDivergence> divergence in divergenceList)
                    {
                        if (divergence.Item2.DivergenceTime > 0)
                        {
                            if (UseConflicts == TopologyConflictMode.NoConflicts || !divergence.Item2.IsConflict)
                                target.ChildDivergences.Add(divergence.Item2);
                        }
                    }
                }

                target.TotalStudies = UPGMANode.totalStudies(besti, bestj, tempNodes);
                target.SupportingStudies = UPGMANode.supportingStudies(besti, bestj, tempNodes);
                target.percentSupport = (double)target.SupportingStudies / (double)target.TotalStudies;
            }            

            if (nodeUpdated || target.ChildDivergences.Count > 0)
                target.UpdateText();
        }

        public void setTimesRootHAL(TopoTimeNode target, StringBuilder log = null)
        {
            List<KeyValuePair<int, Divergence>> validDivergences = partitionFetcher.GetRootPartitions(target).Select(x => new KeyValuePair<int, Divergence>(x.citation, new Divergence(x.DivergenceTime, x.phylogeny_node_id, x.citation))).ToList();

            List<Tuple<double, ChildPairDivergence>> divergenceList = getDivergenceCitations(validDivergences, target, DBService);

            if (divergenceList != null)
            {
                target.ChildDivergences.Clear();
                foreach (Tuple<double, ChildPairDivergence> divergence in divergenceList)
                {
                    if (divergence.Item2.DivergenceTime > 0)
                        target.ChildDivergences.Add(divergence.Item2);
                }
            }

            target.UpdateText();
        }

        public Dictionary<int, Divergence> getDivergenceTimesOnly(IEnumerable<TopoTimeNode> nodeA, IEnumerable<TopoTimeNode> nodeB, IEnumerable<TopoTimeNode> outgroup, out int contradict, ref string list_a, ref string list_b, DatabaseService DBService, bool retainEmptyTimes = false, bool retainConflicts = false)
        {
            contradict = 0;

            if (nodeA.Count() == 0 || nodeB.Count() == 0)
                return null;

            //string list_a;
            //string list_b;
            string list_c = "";

            IEnumerable<int> nodeOutgroup = null;
            if (outgroup != null)
                nodeOutgroup = outgroup.Select(x => x.TaxonID);

            BuildIncludedTaxaList(nodeA.Select(x => x.TaxonID), nodeB.Select(x => x.TaxonID), nodeOutgroup, out list_a, out list_b, out list_c);

            Dictionary<int, Divergence> result = getDivergenceTimeTable(list_a, list_b, list_c, out contradict, DBService, retainEmptyTimes, retainConflicts);
            return result;
        }

        public List<Tuple<double, ChildPairDivergence>> getDivergenceCitations(IEnumerable<KeyValuePair<int, Divergence>> divergences, TopoTimeNode target, DatabaseService DBService)
        {
            List<Tuple<double, ChildPairDivergence>> divTimes = new List<Tuple<double, ChildPairDivergence>>();
            string sql;
            DataTable table;

            foreach (KeyValuePair<int, Divergence> pair in divergences)
            {
                Divergence div = pair.Value;
                // build the metadata tree
                double time = div.DivergenceTime;
                //string phylogeny_node_id = div.PhylogenyNodes;

                List<MyTuple<string, string>> statsData = new List<MyTuple<string, string>>();

                statsData.Add(new Tuple<string, string>("time", div.DivergenceTime.ToString()));
                statsData.Add(new Tuple<string, string>("citation_num", pair.Key.ToString()));
                statsData.Add(new Tuple<string, string>("phylogeny_node", div.PhylogenyNodes.ToString()));

                /*
                if (div.ReferenceID.GetType() != typeof(DBNull))
                    statsData.Add(new Tuple<string, string>("pubmed_id", div.ReferenceID.ToString()));
                else
                    statsData.Add(new Tuple<string, string>("pubmed_id", div.PubMedID.ToString()));
                statsData.Add(new Tuple<string, string>("phylogeny_node", div.PhylogenyNodes.ToString()));
                statsData.Add(new Tuple<string, string>("author", div.AuthorName));
                statsData.Add(new Tuple<string, string>("year", div.Year.ToString()));
                statsData.Add(new Tuple<string, string>("title", div.PubTitle));
                */

        // find the associated taxa for each list
        /*
        sql = "SELECT DISTINCT t.scientific_name FROM " + phyloTopoTableName + " pt, ncbi_taxonomy t Where pt.i_phylogeny_node_id IN (" + phylogeny_node_id + ") AND pt.taxon_id = t.taxon_id AND pt.taxon_id IN (" + list_a + ");";
        table = getSQLResult(sql, conn);

        foreach (DataRow taxaRow in table.Rows)
            taxaGroupA.Add(taxaRow[0].ToString());

        sql = "SELECT DISTINCT t.scientific_name FROM " + phyloTopoTableName + " pt, ncbi_taxonomy t Where pt.i_phylogeny_node_id IN ( " + phylogeny_node_id + ") AND pt.taxon_id = t.taxon_id AND pt.taxon_id IN (" + list_b + ");";
        table = getSQLResult(sql, conn);

        foreach (DataRow taxaRow in table.Rows)
            taxaGroupB.Add(taxaRow[0].ToString());
            */

        List<int> taxonIDsA = new List<int>();
                List<int> taxonIDsB = new List<int>();

                foreach (int phylogeny_node_id in div.DivergenceTimes.Select(x => x.PhylogenyNodeID))
                {
                    string list_a = String.Join(",", target.Nodes[0].getNamedChildren().Select(x => x.TaxonID));
                    sql = "SELECT DISTINCT taxon_id FROM phylogeny_topology pt JOIN unnest(ARRAY[" + list_a + "]) a ON a = pt.taxon_id AND pt.i_phylogeny_node_id = " + phylogeny_node_id + ";";
                    table = DBService.GetSQLResult(sql);

                    foreach (DataRow taxaRow in table.Rows)
                        taxonIDsA.Add((int)taxaRow[0]);

                    //taxonIDsA.Add(0);
                }

                foreach (int phylogeny_node_id in div.DivergenceTimes.Select(x => x.PhylogenyNodeID))
                {
                    string list_b = String.Join(",", target.Nodes[1].getNamedChildren().Select(x => x.TaxonID));
                    sql = "SELECT DISTINCT taxon_id FROM phylogeny_topology pt JOIN unnest(ARRAY[" + list_b + "]) a ON a = pt.taxon_id AND pt.i_phylogeny_node_id = " + phylogeny_node_id + ";";
                    table = DBService.GetSQLResult(sql);

                    foreach (DataRow taxaRow in table.Rows)
                        taxonIDsB.Add((int)taxaRow[0]);

                    //taxonIDsB.Add(0);
                }



                ChildPairDivergence metadata = new ChildPairDivergence();
                metadata.DivergenceTime = time;
                metadata.StatsData = statsData;
                metadata.TaxonIDsA = taxonIDsA;
                metadata.TaxonIDsB = taxonIDsB;
                metadata.IsConflict = div.IsConflict;

                divTimes.Add(new Tuple<double, ChildPairDivergence>(time, metadata));
            }

            return divTimes;
        }

        public static Dictionary<int, Divergence> getDivergenceTimeTable(string list_a, string list_b, string list_outgroup, out int contradict, DatabaseService DBService, bool retainEmptyTimes = false, bool retainConflicts = false)
        {
            string sql;
            DataTable table;
            contradict = 0;

            Dictionary<int, Divergence> result = new Dictionary<int, Divergence>();

            if (list_a == "" || list_b == "")
                return result;

            if (list_outgroup != "")
            {
                sql = "SELECT * FROM divergence_time_original_table2(ARRAY[" + list_a + "], ARRAY[" + list_b + "], ARRAY[" + list_outgroup + "]);";
            }
            else
            {
                sql = "SELECT * FROM divergence_time_original_table2(ARRAY[" + list_a + "], ARRAY[" + list_b + "]);";
            }

            table = DBService.GetSQLResult(sql);

            for (int m = 0; m < table.Rows.Count; m++)
            {
                // The intersect query returns multiple nodes per study tree, however only one is representative of the divergence we are looking for
                // We cannot simply assume that the node that is not a parent of any other is the MRCA
                // Because a higher-up node might include some additional input taxa

                // This part goes through all the nodes and looks at each 

                // 12 = checks that node contains no taxa from outgroup C
                // 13 = checks that group A contains no taxa from group B
                // 14 = checks that group B contains no taxa from group A
                // 3 = total number of taxa that node contains in any of group A or B
                // 2 = parent phylogeny id

                // 12, 13, and 14 must be null
                // 4 must be the same for the target node as well as the root of the tree

                // this function assumes that the results are properly sorted and that all i_phylogeny_node_id < i_parent_phylogeny_node_id
                // also that every tree ends somewhere, with null i_parent_phylogeny_node_id 

                // 0 - f_time_estimate
                // 1 - i_phylogeny_node_id integer
                // 2 - i_parent_phylogeny_node_id integer
                // 3 - count bigint
                // 4 - i_citation_num integer
                // 5 - child_node_id_a integer
                // 6 - child_node_id_b integer
                // 7 - includes_outgroup integer, 
                // 8 - mutually_exclusive_a integer
                // 9 - mutually_exclusive_b integer
                DataRow row = table.Rows[m];
                if (row[8].GetType() == typeof(DBNull) && row[9].GetType() == typeof(DBNull))
                {
                    while (table.Rows[m][2].GetType() != typeof(DBNull))
                    {
                        m++;
                    }

                    bool containsOutgroup = row[7].GetType() != typeof(DBNull);

                    double divergenceTime = (double)row[0];
                    int phylogenyNodeID = (int)row[1];
                    int count = (int)(long)row[3];
                    int citationID = (int)row[4];


                    // check to make sure that no taxa from groups A or B are found outside of the node
                    // if yes, this node contradicts and must be excluded

                    // we can include timeless nodes, but only if the topology of the current tree and source tree match
                    if ((int)(long)table.Rows[m][3] == count && ((retainEmptyTimes && !containsOutgroup) || divergenceTime > 0))
                    {
                        if (retainConflicts || (!retainConflicts && !containsOutgroup))
                        {
                            //int year = row[9] is int ? (int)row[9] : 0;

                            Divergence div = new Divergence(divergenceTime, phylogenyNodeID, citationID, IsConflict: containsOutgroup);

                            if (!result.ContainsKey(citationID))
                                result.Add(citationID, div);
                            else
                                result[citationID].AddDivergence(divergenceTime, phylogenyNodeID, containsOutgroup);
                        }
                    }
                    else
                    {
                        contradict++;
                    }
                }
                else
                {
                    // this study node contradicts the proposed phylogeny and is excluded from the time results
                    contradict++;

                    while (table.Rows[m][2].GetType() != typeof(DBNull) && m < table.Rows.Count - 1)
                    {
                        m++;
                    }
                }
            }

            return result;
        }


        // 11-12-2020: This function does the job only if storedNamedNodes is initialized properly (may not occur if an older tree is loaded) and if the supertree contains ALL the necessary taxa
        public void BuildIncludedTaxaList(IEnumerable<int> leavesA, IEnumerable<int> leavesB, IEnumerable<int> leavesC, out string list_a, out string list_b, out string list_c)
        {
            list_a = String.Join(",", leavesA);
            list_b = String.Join(",", leavesB);
            list_c = leavesC != null && leavesC.Any() ? String.Join(",", leavesC) : "";
        }

        // 11-12-2020: This function may no longer be necessary and cause conflicts, now that a) we store most of this internally, in the storedNamedNodes variable, and b) we preprocess our trees to exclude subspecies... in theory.
        // Apparently some subspecies have escaped this pruning process and will be revisited
        public void BuildIncludedTaxaList(IEnumerable<int> leavesA, IEnumerable<int> leavesB, IEnumerable<int> leavesC, DatabaseService DBService, out string list_a, out string list_b, out string list_c)
        {
            string initialListA = String.Join(",", leavesA);
            string initialListB = String.Join(",", leavesB);
            string initialListC = leavesC != null && leavesC.Any() ? String.Join(",", leavesC) : "";

            string sql;

            // find the common ancestors of all taxa involved
            // from there, go one step down to find the NCBI node representing each group
            // this is done by the ncbi_common_ancestor and most_inclusive_parent SQL functions

            // However, this technique does not work if our tree phylogeny is no longer consistent with NCBI
            // Need some way to check the monophyly of both groups
            // if group A contains some taxa within group B or vice-versa,
            // Also if the outgroup contains taxa found in either
            // in that case, simply include the species and their descendants
            // Actually, if we have an outgroup at all we should just do that

            if (initialListC == "")
            {
                sql = "SELECT taxon_id FROM ncbi_common_ancestor(ARRAY[" + initialListA + "," + initialListB + "])";
                object commonAncestor = DBService.GetSingleSQL(sql);

                sql = "SELECT string_agg(DISTINCT d.taxon_id::text, ',') FROM descendant_tt_taxonomy_list((SELECT array_agg(DISTINCT taxon_id) FROM most_inclusive_parent(ARRAY[" + initialListA + "], " + commonAncestor + "))) d JOIN phylogeny_topology pt ON pt.taxon_id=d.taxon_id";
                list_a = DBService.GetSingleSQL(sql).ToString();
                sql = "SELECT string_agg(DISTINCT d.taxon_id::text, ',') FROM descendant_tt_taxonomy_list((SELECT array_agg(DISTINCT taxon_id) FROM most_inclusive_parent(ARRAY[" + initialListB + "], " + commonAncestor + "))) d JOIN phylogeny_topology pt ON pt.taxon_id=d.taxon_id";
                list_b = DBService.GetSingleSQL(sql).ToString();
                list_c = "";
            }
            else
            {
                sql = "SELECT string_agg(DISTINCT d.taxon_id::text, ',') FROM descendant_tt_taxonomy_list(ARRAY[" + initialListA + "]) d JOIN phylogeny_topology pt ON pt.taxon_id=d.taxon_id";
                list_a = DBService.GetSingleSQL(sql).ToString();
                sql = "SELECT string_agg(DISTINCT d.taxon_id::text, ',') FROM descendant_tt_taxonomy_list(ARRAY[" + initialListB + "]) d JOIN phylogeny_topology pt ON pt.taxon_id=d.taxon_id";
                list_b = DBService.GetSingleSQL(sql).ToString();
                sql = "SELECT string_agg(DISTINCT taxon_id::text, ',') FROM descendant_tt_taxonomy_list(ARRAY[" + initialListC + "]);";
                list_c = DBService.GetSingleSQL(sql).ToString();
            }

        }

        
    }
}
