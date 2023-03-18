using System.Collections;

namespace Meander;

/// <summary>
/// This Class implements the Difference Algorithm published in
/// "An O(ND) Difference Algorithm and its Variations" by Eugene Myers
/// Algorithmica Vol. 1 No. 2, 1986, p 251.
///
/// Copyright (c) by Matthias Hertel, http://www.mathertel.de
/// All rights reserved.
///
/// Redistribution and use in source and binary forms, with or without modification,
/// are permitted provided that the following conditions are met:
///
/// Redistributions of source code must retain the above copyright notice,
/// this list of conditions and the following disclaimer.
/// Redistributions in binary form must reproduce the above copyright notice,
/// this list of conditions and the following disclaimer in the documentation
/// and/or other materials provided with the distribution.
/// Neither the name of the copyright owners nor the names of its contributors
/// may be used to endorse or promote products derived from this software without
/// specific prior written permission.
///
/// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
/// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
/// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
/// IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
/// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
/// BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
/// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
/// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
/// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
/// </summary>
public static class Diff
{
    public static Item[] Find(IEnumerable original, IEnumerable @new, IEqualityComparer comparer = null)
    {
        if (original == null)
            throw new ArgumentNullException(nameof(original));
        if (@new == null)
            throw new ArgumentNullException(nameof(@new));

        if (comparer == null)
            comparer = EqualityComparer<object>.Default;

        // prepare the input sequences and convert to comparable numbers.
        var h = new Hashtable(comparer);
        // The A-Version of the data (original data) to be compared.
        var dataA = new DiffData(DiffCodes(original, h));
        // The B-Version of the data (modified data) to be compared.
        var dataB = new DiffData(DiffCodes(@new, h));
        // Free hash-table memory
        h.Clear();
        h = null;

        var max = 2 * (dataA.Length + dataB.Length) + 4;
        // vector for the (0,0) to (x,y) search
        var DownVector = new int[max];
        // vector for the (u,v) to (N,M) search
        var UpVector = new int[max];

        LCS(dataA, 0, dataA.Length, dataB, 0, dataB.Length, DownVector, UpVector);
        return CreateDiffs(dataA, dataB);
    }

    /// <summary>Scan the tables of which lines are inserted and deleted,
    /// producing an edit script in forward order.
    /// </summary>
    /// dynamic array
    private static Item[] CreateDiffs(DiffData DataA, DiffData DataB)
    {
        var a = new List<Item>();

        int StartA, StartB;
        int LineA, LineB;

        LineA = 0;
        LineB = 0;
        while (LineA < DataA.Length || LineB < DataB.Length)
        {
            if ((LineA < DataA.Length) && (!DataA.modified[LineA])
              && (LineB < DataB.Length) && (!DataB.modified[LineB]))
            {
                // equal lines
                LineA++;
                LineB++;
            }
            else
            {
                // maybe deleted and/or inserted lines
                StartA = LineA;
                StartB = LineB;

                while (LineA < DataA.Length && (LineB >= DataB.Length || DataA.modified[LineA]))
                    LineA++;

                while (LineB < DataB.Length && (LineA >= DataA.Length || DataB.modified[LineB]))
                    LineB++;

                if ((StartA < LineA) || (StartB < LineB))
                {
                    // store a new difference-item
                    a.Add(new Item
                    {
                        StartA = StartA,
                        StartB = StartB,
                        deletedA = LineA - StartA,
                        insertedB = LineB - StartB
                    });
                }
            }
        }
        return a.ToArray();
    }

    /// <summary>
    /// This function converts all <paramref name="values"/> into unique numbers for every unique value
    /// so further work can be done only with simple numbers.
    /// </summary>
    /// <returns>a list of integers.</returns>
    private static IList<int> DiffCodes(IEnumerable values, Hashtable h)
    {
        var result = new List<int>();
        var lastUsedCode = h.Count;

        foreach (var value in values)
        {
            var code = h[value];

            if (code == null)
            {
                h[value] = ++lastUsedCode;
                result.Add(lastUsedCode);
            }
            else
            {
                result.Add((int)code);
            }
        }
        return result;
    }

    /// <summary>
    /// This is the divide-and-conquer implementation of the longes common-subsequence (LCS)
    /// algorithm.
    /// The published algorithm passes recursively parts of the A and B sequences.
    /// To avoid copying these arrays the lower and upper bounds are passed while the sequences stay constant.
    /// </summary>
    /// <param name="DataA">sequence A</param>
    /// <param name="LowerA">lower bound of the actual range in DataA</param>
    /// <param name="UpperA">upper bound of the actual range in DataA (exclusive)</param>
    /// <param name="DataB">sequence B</param>
    /// <param name="LowerB">lower bound of the actual range in DataB</param>
    /// <param name="UpperB">upper bound of the actual range in DataB (exclusive)</param>
    /// <param name="DownVector">a vector for the (0,0) to (x,y) search. Passed as a parameter for speed reasons.</param>
    /// <param name="UpVector">a vector for the (u,v) to (N,M) search. Passed as a parameter for speed reasons.</param>
    private static void LCS(DiffData DataA, int LowerA, int UpperA, DiffData DataB, int LowerB, int UpperB, int[] DownVector, int[] UpVector)
    {
        // Fast walkthrough equal lines at the start
        while (LowerA < UpperA && LowerB < UpperB && DataA[LowerA] == DataB[LowerB])
        {
            LowerA++;
            LowerB++;
        }
        // Fast walkthrough equal lines at the end
        while (LowerA < UpperA && LowerB < UpperB && DataA[UpperA - 1] == DataB[UpperB - 1])
        {
            --UpperA;
            --UpperB;
        }
        if (LowerA == UpperA)
        {
            // mark as inserted lines.
            while (LowerB < UpperB)
                DataB.modified[LowerB++] = true;
        }
        else if (LowerB == UpperB)
        {
            // mark as deleted lines.
            while (LowerA < UpperA)
                DataA.modified[LowerA++] = true;
        }
        else
        {
            // Find the middle snakea and length of an optimal path for A and B
            var smsrd = SMS(DataA, LowerA, UpperA, DataB, LowerB, UpperB, DownVector, UpVector);

            // The path is from LowerX to (x,y) and (x,y) to UpperX
            LCS(DataA, LowerA, smsrd.x, DataB, LowerB, smsrd.y, DownVector, UpVector);
            LCS(DataA, smsrd.x, UpperA, DataB, smsrd.y, UpperB, DownVector, UpVector);
        }
    }

    /// <summary>
    /// This is the algorithm to find the Shortest Middle Snake (SMS).
    /// </summary>
    /// <param name="DataA">sequence A</param>
    /// <param name="LowerA">lower bound of the actual range in DataA</param>
    /// <param name="UpperA">upper bound of the actual range in DataA (exclusive)</param>
    /// <param name="DataB">sequence B</param>
    /// <param name="LowerB">lower bound of the actual range in DataB</param>
    /// <param name="UpperB">upper bound of the actual range in DataB (exclusive)</param>
    /// <param name="DownVector">a vector for the (0,0) to (x,y) search. Passed as a parameter for speed reasons.</param>
    /// <param name="UpVector">a vector for the (u,v) to (N,M) search. Passed as a parameter for speed reasons.</param>
    /// <returns>a MiddleSnakeData record containing x,y and u,v</returns>
    private static SMSRD SMS(DiffData DataA, int LowerA, int UpperA, DiffData DataB, int LowerB, int UpperB,
      int[] DownVector, int[] UpVector)
    {
        SMSRD ret;
        int MAX = DataA.Length + DataB.Length + 1;

        int DownK = LowerA - LowerB; // the k-line to start the forward search
        int UpK = UpperA - UpperB; // the k-line to start the reverse search

        int Delta = (UpperA - LowerA) - (UpperB - LowerB);
        bool oddDelta = (Delta & 1) != 0;

        // The vectors in the publication accepts negative indexes. the vectors implemented here are 0-based
        // and are access using a specific offset: UpOffset UpVector and DownOffset for DownVektor
        int DownOffset = MAX - DownK;
        int UpOffset = MAX - UpK;

        int MaxD = ((UpperA - LowerA + UpperB - LowerB) / 2) + 1;

        // init vectors
        DownVector[DownOffset + DownK + 1] = LowerA;
        UpVector[UpOffset + UpK - 1] = UpperA;

        for (int D = 0; D <= MaxD; D++)
        {
            // Extend the forward path.
            for (int k = DownK - D; k <= DownK + D; k += 2)
            {
                // find the only or better starting point
                int x, y;
                if (k == DownK - D)
                {
                    x = DownVector[DownOffset + k + 1]; // down
                }
                else
                {
                    x = DownVector[DownOffset + k - 1] + 1; // a step to the right
                    if ((k < DownK + D) && (DownVector[DownOffset + k + 1] >= x))
                        x = DownVector[DownOffset + k + 1]; // down
                }
                y = x - k;

                // find the end of the furthest reaching forward D-path in diagonal k.
                while ((x < UpperA) && (y < UpperB) && (DataA[x] == DataB[y]))
                {
                    x++; y++;
                }
                DownVector[DownOffset + k] = x;

                // overlap ?
                if (oddDelta && (UpK - D < k) && (k < UpK + D))
                {
                    if (UpVector[UpOffset + k] <= DownVector[DownOffset + k])
                    {
                        ret.x = DownVector[DownOffset + k];
                        ret.y = DownVector[DownOffset + k] - k;

                        return (ret);
                    }
                }
            }

            // Extend the reverse path.
            for (int k = UpK - D; k <= UpK + D; k += 2)
            {
                // find the only or better starting point
                int x, y;
                if (k == UpK + D)
                {
                    x = UpVector[UpOffset + k - 1]; // up
                }
                else
                {
                    x = UpVector[UpOffset + k + 1] - 1; // left
                    if ((k > UpK - D) && (UpVector[UpOffset + k - 1] < x))
                        x = UpVector[UpOffset + k - 1]; // up
                }
                y = x - k;

                while ((x > LowerA) && (y > LowerB) && (DataA[x - 1] == DataB[y - 1]))
                {
                    x--; y--; // diagonal
                }
                UpVector[UpOffset + k] = x;

                // overlap ?
                if (!oddDelta && (DownK - D <= k) && (k <= DownK + D))
                {
                    if (UpVector[UpOffset + k] <= DownVector[DownOffset + k])
                    {
                        ret.x = DownVector[DownOffset + k];
                        ret.y = DownVector[DownOffset + k] - k;

                        return (ret);
                    }
                }
            }
        }
        throw new InvalidOperationException("the algorithm should never come here.");
    }

    /// <summary>
    /// Data on one input file being compared.
    /// </summary>
    private class DiffData
    {
        /// <summary>
        /// Array of booleans that flag for modified data.
        /// This is the result of the diff.
        /// This means deletedA in the first Data or inserted in the second Data.
        /// </summary>
        public readonly bool[] modified;

        /// <summary>Buffer of numbers that will be compared.</summary>
        private readonly IList<int> _data;

        /// <summary>Number of elements (lines).</summary>
        public int Length => _data.Count;

        public int this[int index] => _data[index];

        internal DiffData(IList<int> initData)
        {
            _data = initData;
            modified = new bool[initData.Count + 2];
        }
    }

    /// <summary>details of one difference.</summary>
    public struct Item
    {
        /// <summary>Number of changes in Data A.</summary>
        public int deletedA;

        /// <summary>Number of changes in Data B.</summary>
        public int insertedB;

        /// <summary>Start Line number in Data A.</summary>
        public int StartA;

        /// <summary>Start Line number in Data B.</summary>
        public int StartB;
    }

    /// <summary>
    /// Shortest Middle Snake Return Data
    /// </summary>
    private struct SMSRD
    {
        public int x, y;
    }
}