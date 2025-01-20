﻿// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

/// <summary>
/// Provides methods for matching file system names.
/// </summary>
internal class WildcardMatcher : IWildcardMatcher
{
    // [MS - FSA] 2.1.4.4 Algorithm for Determining if a FileName Is in an Expression
    // https://msdn.microsoft.com/en-us/library/ff469270.aspx
    private static readonly char[] WildcardChars = ['\"', '<', '>', '*', '?'];

    private static readonly char[] SimpleWildcardChars = ['*', '?'];

    // Matching routine description
    // ============================
    // (copied from native impl)
    //
    // This routine compares a Dbcs name and an expression and tells the caller
    // if the name is in the language defined by the expression.  The input name
    // cannot contain wildcards, while the expression may contain wildcards.
    //
    // Expression wild cards are evaluated as shown in the nondeterministic
    // finite automatons below.  Note that ~* and ~? are DOS_STAR and DOS_QM.
    //
    //        ~* is DOS_STAR, ~? is DOS_QM, and ~. is DOS_DOT
    //
    //                                  S
    //                               <-----<
    //                            X  |     |  e       Y
    //        X * Y ==       (0)----->-(1)->-----(2)-----(3)
    //
    //                                 S-.
    //                               <-----<
    //                            X  |     |  e       Y
    //        X ~* Y ==      (0)----->-(1)->-----(2)-----(3)
    //
    //                           X     S     S     Y
    //        X ?? Y ==      (0)---(1)---(2)---(3)---(4)
    //
    //                           X     .        .      Y
    //        X ~.~. Y ==    (0)---(1)----(2)------(3)---(4)
    //                              |      |________|
    //                              |           ^   |
    //                              |_______________|
    //                                 ^EOF or .^
    //
    //                           X     S-.     S-.     Y
    //        X ~?~? Y ==    (0)---(1)-----(2)-----(3)---(4)
    //                              |      |________|
    //                              |           ^   |
    //                              |_______________|
    //                                 ^EOF or .^
    //
    //    where S is any single character
    //          S-. is any single character except the final .
    //          e is a null character transition
    //          EOF is the end of the name string
    //
    //   In words:
    //
    //       * matches 0 or more characters.
    //       ? matches exactly 1 character.
    //       DOS_STAR matches 0 or more characters until encountering and matching
    //           the final . in the name.
    //       DOS_QM matches any single character, or upon encountering a period or
    //           end of name string, advances the expression to the end of the
    //           set of contiguous DOS_QMs.
    //       DOS_DOT matches either a . or zero characters beyond name string.

    public bool Match(
        ReadOnlySpan<char> wildcard,
        ReadOnlySpan<char> text,
        bool ignoreCase = false,
        bool useExtendedWildcards = false)
    {
        // The idea behind the algorithm is pretty simple. We keep track of all possible locations
        // in the regular expression that are matching the name. When the name has been exhausted,
        // if one of the locations in the expression is also just exhausted, the name is in the
        // language defined by the regular expression.
        if (wildcard.Length == 0 || text.Length == 0)
        {
            return false;
        }

        if (wildcard[0] == '*')
        {
            // Just * matches everything
            if (wildcard.Length == 1)
            {
                return true;
            }

            var expressionEnd = wildcard[1..];
            if (expressionEnd.IndexOfAny(useExtendedWildcards ? WildcardChars : SimpleWildcardChars) == -1)
            {
                // Handle the special case of a single starting *, which essentially means "ends with"

                // If the name doesn't have enough characters to match the remaining expression, it can't be a match.
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (text.Length < expressionEnd.Length)
                {
                    return false;
                }

                // See if we end with the expression
                return text.EndsWith(expressionEnd, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            }
        }

        var nameOffset = 0;
        var matchCount = 1;
        var nameChar = '\0';

        // ReSharper disable once RedundantAssignment
        // ReSharper disable once UseCollectionExpression
        Span<int> temp = stackalloc int[0];
        Span<int> currentMatches = stackalloc int[16];
        Span<int> priorMatches = stackalloc int[16];
        priorMatches[0] = 0;

        var maxState = wildcard.Length * 2;
        int currentState;
        var nameFinished = false;

        //  Walk through the name string, picking off characters.  We go one
        //  character beyond the end because some wild cards are able to match
        //  zero characters beyond the end of the string.
        //
        //  With each new name character we determine a new set of states that
        //  match the name so far.  We use two arrays that we swap back and forth
        //  for this purpose.  One array lists the possible expression states for
        //  all name characters up to but not including the current one, and other
        //  array is used to build up the list of states considering the current
        //  name character as well.  The arrays are then switched and the process
        //  repeated.
        //
        //  There is not a one-to-one correspondence between state number and
        //  offset into the expression. State numbering is not continuous.
        //  This allows a simple conversion between state number and expression
        //  offset.  Each character in the expression can represent one or two
        //  states.  * and DOS_STAR generate two states: expressionOffset * 2 and
        //  expressionOffset * 2 + 1.  All other expression characters can produce
        //  only a single state. Thus, expressionOffset = currentState / 2.

        while (!nameFinished)
        {
            if (nameOffset < text.Length)
            {
                // Not at the end of the name. Grab the current character and move the offset forward.
                nameChar = text[nameOffset++];
            }
            else
            {
                // At the end of the name. If the expression is exhausted, exit.
                if (priorMatches[matchCount - 1] == maxState)
                    break;

                nameFinished = true;
            }

            // Now, for each of the previous stored expression matches, see what
            // we can do with this name character.
            var priorMatch = 0;
            var currentMatch = 0;
            var priorMatchCount = 0;

            while (priorMatch < matchCount)
            {
                // We have to carry on our expression analysis as far as possible for each
                // character of name, so we loop here until the expression stops matching.
                var expressionOffset = (priorMatches[priorMatch++] + 1) / 2;

                while (expressionOffset < wildcard.Length)
                {
                    currentState = expressionOffset * 2;
                    var expressionChar = wildcard[expressionOffset];

                    // We may be about to exhaust the local space for matches,
                    // so we have to reallocate if this is the case.
                    if (currentMatch >= currentMatches.Length - 2)
                    {
                        var newSize = currentMatches.Length * 2;
                        temp = new int[newSize];
                        currentMatches.CopyTo(temp);
                        currentMatches = temp;

                        temp = new int[newSize];
                        priorMatches.CopyTo(temp);
                        priorMatches = temp;
                    }

                    if (expressionChar == '*')
                    {
                        // '*' matches any character zero or more times.
                        goto MatchZeroOrMore;
                    }

                    if (useExtendedWildcards && expressionChar == '<')
                    {
                        // '<' (DOS_STAR) matches any character except '.' zero or more times.

                        // If we are at a period, determine if we are allowed to
                        // consume it, i.e. make sure it is not the last one.

                        var notLastPeriod = false;
                        if (!nameFinished && nameChar == '.')
                        {
                            for (var offset = nameOffset; offset < text.Length; offset++)
                            {
                                // ReSharper disable once InvertIf
                                if (text[offset] == '.')
                                {
                                    notLastPeriod = true;
                                    break;
                                }
                            }
                        }

                        if (nameFinished || nameChar != '.' || notLastPeriod)
                        {
                            goto MatchZeroOrMore;
                        }

                        // We are at a period.  We can only match zero
                        // characters (i.e. the epsilon transition).
                        goto MatchZero;
                    }

                    // The remaining expression characters all match by consuming a character,
                    // so we need to force the expression and state forward.
                    currentState += 2;

                    switch (useExtendedWildcards)
                    {
                        case true when expressionChar == '>':
                        {
                            // '>' (DOS_QM) is the most complicated. If the name is finished,
                            // we can match zero characters. If this name is a '.', we
                            // don't match, but look at the next expression.  Otherwise
                            // we match a single character.
                            if (nameFinished || nameChar == '.')
                                goto NextExpressionCharacter;

                            currentMatches[currentMatch++] = currentState;
                            goto ExpressionFinished;
                        }

                        case true when expressionChar == '"':
                        {
                            // A '"' (DOS_DOT) can match either a period, or zero characters
                            // beyond the end of name.
                            if (nameFinished)
                            {
                                goto NextExpressionCharacter;
                            }

                            if (nameChar == '.')
                            {
                                currentMatches[currentMatch++] = currentState;
                            }

                            goto ExpressionFinished;
                        }

                        default:
                        {
                            if (expressionChar == '\\')
                            {
                                // Escape character, try to move the expression forward again and match literally.
                                if (++expressionOffset == wildcard.Length)
                                {
                                    currentMatches[currentMatch++] = maxState;
                                    goto ExpressionFinished;
                                }

                                currentState = expressionOffset * 2 + 2;
                                expressionChar = wildcard[expressionOffset];
                            }

                            // From this point on a name character is required to even
                            // continue, let alone make a match.
                            if (nameFinished)
                            {
                                goto ExpressionFinished;
                            }

                            if (expressionChar == '?')
                            {
                                // If this expression was a '?' we can match it once.
                                currentMatches[currentMatch++] = currentState;
                            }
                            else
                            {
                                if (ignoreCase
                                        ? char.ToUpperInvariant(expressionChar) == char.ToUpperInvariant(nameChar)
                                        : expressionChar == nameChar)
                                {
                                    // Matched a non-wildcard character
                                    currentMatches[currentMatch++] = currentState;
                                }
                            }

                            goto ExpressionFinished;
                        }
                    }

                    MatchZeroOrMore:
                    currentMatches[currentMatch++] = currentState;
                    MatchZero:
                    currentMatches[currentMatch++] = currentState + 1;
                    NextExpressionCharacter:
                    if (++expressionOffset == wildcard.Length)
                    {
                        currentMatches[currentMatch++] = maxState;
                    }
                } // while (expressionOffset < expression.Length)

                ExpressionFinished:

                // Prevent duplication in the destination array.
                //
                // Each of the arrays is monotonically increasing and non-duplicating, thus we skip
                // over any source element in the source array if we just added the same element to
                // the destination array. This guarantees non-duplication in the destination array.

                // ReSharper disable once InvertIf
                if (priorMatch < matchCount && priorMatchCount < currentMatch)
                {
                    while (priorMatchCount < currentMatch)
                    {
                        var previousLength = priorMatches.Length;
                        while (priorMatch < previousLength && priorMatches[priorMatch] < currentMatches[priorMatchCount])
                        {
                            priorMatch++;
                        }

                        priorMatchCount++;
                    }
                }
            } // while (sourceCount < matchesCount)

            // If we found no matches in the just finished iteration it's time to bail.
            if (currentMatch == 0)
            {
                return false;
            }

            // Swap the meaning the two arrays
            temp = priorMatches;
            priorMatches = currentMatches;
            currentMatches = temp;

            matchCount = currentMatch;
        } // while (!nameFinished)

        currentState = priorMatches[matchCount - 1];

        return currentState == maxState;
    }
}