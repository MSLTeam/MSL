using System;

namespace ConPtyTermEmulatorLib
{
    /// <summary>
    /// terminal that will only output text after a specific delimiter is hit and will remove the delmiter
    /// </summary>
    public class ReadDelimitedTerm : Term
    {
        public ReadDelimitedTerm(int READ_BUFFER_SIZE = 1024 * 16, bool USE_BINARY_WRITER = false, ReadOnlySpan<char> delimiter = default, TimeSpan MaxWaitTimeoutForDelimiter = default) : base(READ_BUFFER_SIZE, USE_BINARY_WRITER)
        {
            if (delimiter != default)
                SetReadOutputDelimiter(delimiter, MaxWaitTimeoutForDelimiter);
        }

        override protected Span<char> HandleRead(ref ReadState state)
        {
            var sendSpan = Span<Char>.Empty;
            curBufferOffset += state.readChars;
            var working = state.entireBuffer.Slice(lastDelimEndOffset, curBufferOffset - lastDelimEndOffset);
            var delimPos = working.LastIndexOf(delimiter);
            if (delimPos != -1)
            {
                sendSpan = working.Slice(0, delimPos);
                lastDelimEndOffset += delimPos + delimiter.Length;
            }
            state.curBuffer = state.entireBuffer.Slice(curBufferOffset);
            if (state.curBuffer.Length == 0)
            {
                if (lastDelimEndOffset == 0)
                {//this means the buffer is full so just send it all
                    sendSpan = state.entireBuffer;
                    curBufferOffset = lastDelimEndOffset = 0;
                }
                else
                {//shift everything left
                    var toCopyBlocks = state.entireBuffer.Length - lastDelimEndOffset;
                    var copyWindow = lastDelimEndOffset < toCopyBlocks ? lastDelimEndOffset : toCopyBlocks;
                    var copyPos = lastDelimEndOffset;
                    while (toCopyBlocks > 0)
                    {
                        var dstPos = copyPos - lastDelimEndOffset;
                        var copyAmount = (copyPos + copyWindow) > state.entireBuffer.Length ? state.entireBuffer.Length - copyPos : copyWindow;
                        state.entireBuffer.Slice(copyPos, copyAmount).CopyTo(state.entireBuffer.Slice(dstPos, copyAmount));
                        copyPos += copyAmount;
                        toCopyBlocks -= copyAmount;
                    }
                    curBufferOffset = state.entireBuffer.Length - lastDelimEndOffset;
                    lastDelimEndOffset = 0;
                }
                state.curBuffer = state.entireBuffer.Slice(curBufferOffset);
            }
            return sendSpan;
        }

        private int curBufferOffset = 0; //where in the entirebuffer does the current buffer to read into start
        private int lastDelimEndOffset = 0; //where in the entirebuffer did the last delimiter end should always be <= curBufferOffset, the data between here and curBufferOffset is what is still valid data needing to be sent.


        /// <summary>
        /// Will only send data to the UI Terminal after each delimiter is hit. Caution as the terminal will not get any updates until the delimiter is hit. Pass a 0 length span to disable waiting for a delimiter.   Note if the buffer is completely filled before the delimiter is hit the buffer will be pasesd on.
        /// Note: Delimiter itself is not ever passed
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="MaxWaitForDelimiter"></param>
        public void SetReadOutputDelimiter(ReadOnlySpan<char> delimiter, TimeSpan MaxWaitTimeoutForDelimiter = default)
        {
            this.delimiter = delimiter.ToArray();
            delimiterTimeout = MaxWaitTimeoutForDelimiter;
        }
        private char[] delimiter;
        private TimeSpan delimiterTimeout;

    }
}
