using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeTreeShared;
using TopoTimeShared;

namespace TopoTimeShared
{
    public class UPGMANode
    {
        public Dictionary<int, Divergence>[] distances;
        public TopoTimeNode representative;

        // constructor for new individual node
        public UPGMANode(int priorNodes)
        {
            distances = new Dictionary<int, Divergence>[priorNodes];
        }

        public void AddDivergence(int j, Dictionary<int, Divergence> distance, int contradict)
        {
            distances[j] = distance;
        }

        public void AddDivergence(int j, double divergenceTime, int citation, int phylogeny_node, bool topologyConflict)
        {
            if (distances[j] == null)
                distances[j] = new Dictionary<int, Divergence>();

            if (distances[j].ContainsKey(citation))
                distances[j][citation].AddDivergence(divergenceTime, phylogeny_node, topologyConflict);
            else
                distances[j][citation] = new Divergence(divergenceTime, phylogeny_node, citation, topologyConflict);
        }

        public static bool TopologyConflict(int i, int j, UPGMANode[] matrix)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];
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

        public static bool HasConcordantDivergences(int i, int j, UPGMANode[] matrix)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];
            IEnumerable<KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => x.Value.IsConflict == false);

            return noConflictDivergences.Any();
        }

        public static Dictionary<int, Divergence> divergences(int i, int j, UPGMANode[] matrix)
        {
            return matrix[Math.Max(i, j)].distances[Math.Min(i, j)];
        }

        /*
        public static IEnumerable<KeyValuePair<int, Divergence>> ValidDivergences(int i, int j, UPGMANode[] matrix)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];

            // first, pick out divergences with no time but still define topology
            // these take precedence when all other divergence time entries show topology conflict

            object ZeroConcordantTime = null;
            object OtherConcordantTime = null;

            foreach (KeyValuePair<int, Divergence> divergence in divergences)
            {
                Divergence div = divergence.Value;

                if (!div.IsConflict)
                {
                    if (div.DivergenceTime <= 0)
                    {
                        ZeroConcordantTime = divergence;
                    }
                    else if (div.DivergenceTime > 0)
                    {
                        OtherConcordantTime = divergence;
                    }
                }
            }

            if (ZeroConcordantTime != null && OtherConcordantTime == null)
                yield return (KeyValuePair<int, Divergence>)ZeroConcordantTime;
            else
                foreach (KeyValuePair<int, Divergence> divergence in divergences)
                    yield return divergence;
        }
        */

        public static IEnumerable<KeyValuePair<int, Divergence>> ValidDivergences(int i, int j, UPGMANode[] matrix)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];

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

        public static int countAgreement(int i, int j, UPGMANode[] matrix)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];

            if (divergences == null || !divergences.Any())
                return -1;

            IEnumerable<KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => x.Value.IsConflict == false);
            return noConflictDivergences.Count();

        }

        public static double distance(int i, int j, UPGMANode[] matrix, bool useMedian = false, HALFunctions.TopologyConflictMode UseConflicts = HALFunctions.TopologyConflictMode.NoConflicts, int guideTree = 0)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];

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
            if (hasZeroConcordantTime && !hasOtherConcordantTime)
                return 0;
            else
            {
                IEnumerable<double> nonZeroTimes;

                if (UseConflicts == HALFunctions.TopologyConflictMode.NoConflicts)
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

        /*
        public static double distance(int i, int j, UPGMANode[] matrix)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];

            if (divergences == null || divergences.Count() == 0)
                return Double.NaN;

            // first, pick out divergences with no time but still define topology
            // these take precedence when all other divergence time entries show topology conflict

            bool hasZeroConcordantTime = false;
            bool hasOtherConcordantTime = false;
            bool hasAllConcordantTimes = true;

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
                IEnumerable<double> nonZeroTimes = divergences.Select(x => x.Value.DivergenceTime).Where(y => y > 0);
                if (!nonZeroTimes.Any())
                    return 0;
                else
                    return nonZeroTimes.Average();
            }
        }
        */

        /*
        public static double distance(int i, int j, UPGMANode[] matrix)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];

            if (divergences == null || divergences.Count() == 0)
                return Double.NaN;

            IEnumerable<KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => x.Value.IsConflict == false);

            // return topology-conflicted times only if no other such data exists

            if (noConflictDivergences.Any())
            {
                IEnumerable<double> nonZeroTimes = noConflictDivergences.Select(x => x.Value.DivergenceTime).Where(y => y > 0);
                if (!nonZeroTimes.Any())
                    return 0;
                else
                    return nonZeroTimes.Average();
            }
            else
            {
                IEnumerable<double> nonZeroTimes = divergences.Select(x => x.Value.DivergenceTime).Where(y => y > 0);
                if (!nonZeroTimes.Any())
                    return 0;
                else
                    return nonZeroTimes.Average();
            }            
        }
        */

        /*
         * // old version, utilizing contradictions field?  not sure that's even used anymore
    public static double percentSupport(int i, int j, UPGMANode[] matrix)
    {
        Dictionary<int, Divergence> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];
        if (divergences != null && divergences.Count() > 0)
        {
            double count = divergences.Count();
            double total = count + matrix[Math.Max(i, j)].contradictions[Math.Min(i, j)];
            return count / total;
        }

        return 0;
    }
    */

        public static double percentSupport(int i, int j, UPGMANode[] matrix, int guideTree = 0)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];

            if (divergences == null || !divergences.Any())
                return 0;

            // if guide tree is specified, only exclude non-guide times when guide tree is not present
            bool useGuideTree = guideTree != 0 && divergences.Any(x => x.Value.CitationID == guideTree && x.Value.IsConflict == false);

            double totalDivergences = divergences.Count();
            if (useGuideTree)
                totalDivergences = divergences.Where(x => x.Value.CitationID == guideTree && x.Value.IsConflict == false).Count();

            IEnumerable <KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => (!useGuideTree || guideTree == x.Value.CitationID) && x.Value.IsConflict == false);
            return (double)noConflictDivergences.Count() / totalDivergences;
        }

        public static int supportingStudies(int i, int j, UPGMANode[] matrix)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];

            if (divergences == null || !divergences.Any())
                return 0;

            IEnumerable<KeyValuePair<int, Divergence>> noConflictDivergences = divergences.Where(x => x.Value.IsConflict == false);
            return noConflictDivergences.Count();
        }

        public static int totalStudies(int i, int j, UPGMANode[] matrix)
        {
            IEnumerable<KeyValuePair<int, Divergence>> divergences = matrix[Math.Max(i, j)].distances[Math.Min(i, j)];

            if (divergences == null || !divergences.Any())
                return 0;
            return divergences.Count();
        }


        /*
        public static string logSupportMatrix(UPGMANode[] matrix)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < matrix.Length; i++)
            {
                for (int j = 0; j < matrix[i].contradictions.Length; j++)
                {
                    List<double> divergences = 
                    if (divergences != null && divergences.Count() > 0)
                    {
                        double count = matrix[Math.Max(i, j)].distances[Math.Min(i, j)].Count();
                        double total = count + matrix[Math.Max(i, j)].contradictions[Math.Min(i, j)];
                        return count / total;
                    }

                    sb.Append(String.Format("{0:0.00}", matrix[i].distances[j]) + " ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
        */
    }
}
