using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.Helpers {
    public static class ILCursorExtensions {
        public const int DefaultMaxInstructionSpread = 0x10;

        /// <summary>
        ///   Go to the next best fit match of a given IL sequence, allowing up to <see cref="DefaultMaxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <exception cref="KeyNotFoundException">
        ///   A match could not be found.
        /// </exception>
        ///
        /// <remarks>
        ///   This function picks the next match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.
        /// </remarks>
        public static void GotoNextBestFit(this ILCursor cursor, MoveType moveType, params Func<Instruction, bool>[] predicates)
            => cursor.GotoNextBestFit(moveType, DefaultMaxInstructionSpread, predicates);

        /// <summary>
        ///   Go to the next best fit match of a given IL sequence, allowing up to <paramref name="maxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="maxInstructionSpread">
        ///   The amount of instructions between predicate matches to still consider as a successful match.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <exception cref="KeyNotFoundException">
        ///   A match could not be found.
        /// </exception>
        ///
        /// <remarks>
        ///   This function picks the next match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.
        /// </remarks>
        public static void GotoNextBestFit(this ILCursor cursor, MoveType moveType, int maxInstructionSpread, params Func<Instruction, bool>[] predicates) {
            if (!cursor.TryGotoNextBestFit(moveType, maxInstructionSpread, predicates))
                throw new KeyNotFoundException($"Could not find a matching set of instructions with instruction spread of {maxInstructionSpread}.");
        }

        /// <summary>
        ///   Go to the next best fit match of a given IL sequence, allowing up to <c>0x10</c>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <remarks>
        ///   This function picks the next match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.
        /// </remarks>
        ///
        /// <returns>
        ///   Whether a match has been found, and the cursor has been moved.
        /// </returns>
        public static bool TryGotoNextBestFit(this ILCursor cursor, MoveType moveType, params Func<Instruction, bool>[] predicates)
            => TryGotoNextBestFit(cursor, moveType, DefaultMaxInstructionSpread, predicates);

        /// <summary>
        ///   Go to the next best fit match of a given IL sequence, allowing up to <paramref name="maxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="maxInstructionSpread">
        ///   The amount of instructions between predicate matches to still consider as a successful match.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <remarks>
        ///   This function picks the next match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.
        /// </remarks>
        ///
        /// <returns>
        ///   Whether a match has been found, and the cursor has been moved.
        /// </returns>
        public static bool TryGotoNextBestFit(this ILCursor cursor, MoveType moveType, int maxInstructionSpread, params Func<Instruction, bool>[] predicates)
        {
            if (predicates.Length == 0)
                throw new ArgumentException("No predicates given.");

            if (predicates.Length == 1)
                return cursor.TryGotoNext(moveType, predicates[0]);

            List<(int start, int end)> matchCandidates = new List<(int start, int end)>();

            int initialPosition = cursor.Index;

            // go to each instance of the first predicate
            while (cursor.TryGotoNext(MoveType.Before, predicates[0]))
            {
                // remember where we were and move past the match
                int savedCursorPosition = cursor.Index++;

                // then try to match the rest of the predicates
                bool matchFound = true;
                for (int i = 1; i < predicates.Length; i++)
                {
                    Func<Instruction, bool> matcher = predicates[i];
                    int beforeMoveIndex = cursor.Index;

                    // also make sure we haven't gone further than maxInstructionSpread
                    if (cursor.TryGotoNext(MoveType.After, matcher) && cursor.Index - beforeMoveIndex <= maxInstructionSpread)
                        continue;

                    matchFound = false;
                    break;
                }

                if (matchFound)
                    // remember the start and end indices of the match
                    matchCandidates.Add((savedCursorPosition, cursor.Index));

                // we go again
                // skip the first instance, else we'll get stuck
                cursor.Index = savedCursorPosition + 1;
            }

            // put the cursor back to where it was
            cursor.Index = initialPosition;

            if (matchCandidates.Count == 0)
                // no match :c
                return false;

            // we found a match!
            // pick the one which has the least instruction spread

            (int start, int end) bestMatch = matchCandidates[0];
            matchCandidates.RemoveAt(0);
            int bestMatchDiff = bestMatch.end - bestMatch.start;

            foreach ((int start, int end) matchCandidate in matchCandidates)
            {
                int matchCandidateDiff = matchCandidate.end - matchCandidate.start;

                switch (matchCandidateDiff - bestMatchDiff)
                {
                    // we found a new best candidate
                    case < 0:

                    // we found two identical matches; pick the nearest one
                    case 0 when GetIndexFromMatch(moveType, matchCandidate) - cursor.Index < GetIndexFromMatch(moveType, bestMatch) - cursor.Index:
                        bestMatch = matchCandidate;
                        bestMatchDiff = matchCandidateDiff;
                        break;
                }
            }

            cursor.Index = GetIndexFromMatch(moveType, bestMatch);

            if (moveType is MoveType.AfterLabel)
                cursor.MoveAfterLabels();

            return true;
        }

        /// <summary>
        ///   Go to the previous best fit match of a given IL sequence, allowing up to <see cref="DefaultMaxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <exception cref="KeyNotFoundException">
        ///   A match could not be found.
        /// </exception>
        ///
        /// <remarks>
        ///   This function picks the previous match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.<br/>
        ///
        ///   If <paramref name="moveType"/> is set to <see cref="MoveType.After"/>, the resulting index
        ///   <b>may</b> have moved forwards instead of backwards, if the first predicate matches close to the cursor,
        ///   and the <paramref name="predicates"/> list is long enough.
        /// </remarks>
        public static void GotoPrevBestFit(this ILCursor cursor, MoveType moveType, params Func<Instruction, bool>[] predicates)
            => cursor.GotoPrevBestFit(moveType, DefaultMaxInstructionSpread, predicates);

        /// <summary>
        ///   Go to the previous best fit match of a given IL sequence, allowing up to <paramref name="maxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="maxInstructionSpread">
        ///   The amount of instructions between predicate matches to still consider as a successful match.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <exception cref="KeyNotFoundException">
        ///   A match could not be found.
        /// </exception>
        ///
        /// <remarks>
        ///   This function picks the previous match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.<br/>
        ///
        ///   If <paramref name="moveType"/> is set to <see cref="MoveType.After"/>, the resulting index
        ///   <b>may</b> have moved forwards instead of backwards, if the first predicate matches close to the cursor,
        ///   and the <paramref name="predicates"/> list is long enough.
        /// </remarks>
        public static void GotoPrevBestFit(this ILCursor cursor, MoveType moveType, int maxInstructionSpread, params Func<Instruction, bool>[] predicates) {
            if (!cursor.TryGotoPrevBestFit(moveType, maxInstructionSpread, predicates))
                throw new KeyNotFoundException($"Could not find a matching set of instructions with instruction spread of {maxInstructionSpread}.");
        }

        /// <summary>
        ///   Go to the previous best fit match of a given IL sequence, allowing up to <c>0x10</c>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <remarks>
        ///   This function picks the previous match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.<br/>
        ///
        ///   If <paramref name="moveType"/> is set to <see cref="MoveType.After"/>, the resulting index
        ///   <b>may</b> have moved forwards instead of backwards, if the first predicate matches close to the cursor,
        ///   and the <paramref name="predicates"/> list is long enough.
        /// </remarks>
        ///
        /// <returns>
        ///   Whether a match has been found, and the cursor has been moved.
        /// </returns>
        public static bool TryGotoPrevBestFit(this ILCursor cursor, MoveType moveType, params Func<Instruction, bool>[] predicates)
            => TryGotoPrevBestFit(cursor, moveType, DefaultMaxInstructionSpread, predicates);

        /// <summary>
        ///   Go to the previous best fit match of a given IL sequence, allowing up to <paramref name="maxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="maxInstructionSpread">
        ///   The amount of instructions between predicate matches to still consider as a successful match.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <remarks>
        ///   This function picks the previous match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.<br/>
        ///
        ///   If <paramref name="moveType"/> is set to <see cref="MoveType.After"/>, the resulting index
        ///   <b>may</b> have moved forwards instead of backwards, if the first predicate matches close to the cursor,
        ///   and the <paramref name="predicates"/> list is long enough.
        /// </remarks>
        ///
        /// <returns>
        ///   Whether a match has been found, and the cursor has been moved.
        /// </returns>
        public static bool TryGotoPrevBestFit(this ILCursor cursor, MoveType moveType, int maxInstructionSpread, params Func<Instruction, bool>[] predicates)
        {
            if (predicates.Length == 0)
                throw new ArgumentException("No predicates given.");

            if (predicates.Length == 1)
                return cursor.TryGotoNext(moveType, predicates[0]);

            List<(int start, int end)> matchCandidates = new List<(int start, int end)>();

            int initialPosition = cursor.Index;

            // go to each instance of the first predicate
            while (cursor.TryGotoPrev(MoveType.Before, predicates[0]))
            {
                // remember where we were and move past the match
                int savedCursorPosition = cursor.Index++;

                // then try to match the rest of the predicates
                bool matchFound = true;
                for (int i = 1; i < predicates.Length; i++)
                {
                    Func<Instruction, bool> matcher = predicates[i];
                    int beforeMoveIndex = cursor.Index;

                    // also make sure we haven't gone further than maxInstructionSpread
                    // note that we could have gone past the initial cursor position!
                    if (cursor.TryGotoNext(MoveType.After, matcher) && cursor.Index - beforeMoveIndex <= maxInstructionSpread)
                        continue;

                    matchFound = false;
                    break;
                }

                if (matchFound)
                    // remember the start and end indices of the match
                    matchCandidates.Add((savedCursorPosition, cursor.Index));

                // we go again
                cursor.Index = savedCursorPosition;
            }

            // put the cursor back to where it was
            cursor.Index = initialPosition;

            if (matchCandidates.Count == 0)
                // no match :c
                return false;

            // we found a match!
            // pick the one which has the least instruction spread

            (int start, int end) bestMatch = matchCandidates[0];
            matchCandidates.RemoveAt(0);
            int bestMatchDiff = bestMatch.end - bestMatch.start;

            foreach ((int start, int end) matchCandidate in matchCandidates)
            {
                int matchCandidateDiff = matchCandidate.end - matchCandidate.start;

                switch (matchCandidateDiff - bestMatchDiff)
                {
                    // we found a new best candidate
                    case < 0:

                    // we found two identical matches; pick the nearest one
                    case 0 when cursor.Index - GetIndexFromMatch(moveType, matchCandidate) < cursor.Index - GetIndexFromMatch(moveType, bestMatch):
                        bestMatch = matchCandidate;
                        bestMatchDiff = matchCandidateDiff;
                        break;
                }
            }

            cursor.Index = GetIndexFromMatch(moveType, bestMatch);

            if (moveType is MoveType.AfterLabel)
                cursor.MoveAfterLabels();

            return true;
        }

        private static int GetIndexFromMatch(MoveType moveType, (int start, int end) match)
            => moveType == MoveType.After ? match.end : match.start;
    }
}
