using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// todo: try-catch-throw

namespace HBLib.Utils
{
    public class FFMpegWrapper
    {
        public string ffPath;

        public FFMpegWrapper(string ffPath)
        {
            this.ffPath = ffPath;
        }

        public double GetDuration(string fn)
        {
            var cmd = new CMDWithOutput();
            fn = Path.GetFullPath(fn);

            // input: ffmpeg -i input.webm
            // output: ... Duration: 01:40:06.08, start: 0.000000, bitrate: 22828 kb/s ...
            var output = cmd.runCMD(ffPath, $"-i \"{fn}\"");

            var pattern = @"Duration: ([^,]+),";
            var rgx = new Regex(pattern);

            var m = Regex.Matches(output, pattern)[0];
            var captured = m.Groups[1].ToString();

            bool isValidTimeSpan;
            try
            {
                TimeSpan.Parse(captured);
                isValidTimeSpan = true;
            }
            catch
            {
                //   Duration: N/A, start: 0.000000, bitrate: N/A
                isValidTimeSpan = false;
            }

            if (!isValidTimeSpan)
            {
                // https://stackoverflow.com/questions/37731524/cannot-retrieve-duration-of-webm-file-using-ffmpeg
                // input: ffmpeg -i input.webm -f null -
                // output: ... frame= 2087 fps=0.0 q=-0.0 Lsize=N/A time=00:01:23.48 bitrate=N/A speed= 123x ...
                pattern = @"time=(.+)\s?bitrate";
                rgx = new Regex(pattern);
                output = cmd.runCMD(ffPath, $"-i \"{fn}\" -f null -");

                var matches = Regex.Matches(output, pattern);
                var match = matches[matches.Count - 1];
                captured = match.Groups[1].ToString();
            }

            var ts = TimeSpan.Parse(captured);
            return ts.TotalSeconds;
        }

        public string VideoToWav(string videoFn, string audioFn)
        {
            videoFn = Path.GetFullPath(videoFn);
            audioFn = Path.GetFullPath(audioFn);
            var cmd = new CMDWithOutput();
            return cmd.runCMD(ffPath,
                $@"-i {videoFn} -acodec pcm_s16le -ac 1 -ar 16000 -fflags +bitexact -flags:v +bitexact -flags:a +bitexact {audioFn}");
        }

        public List<Dictionary<string, string>> SplitBySeconds(string fn, double seconds, string directory = null,
            string rootFn = "split", bool convertToWebm = false)
        {
            fn = Path.GetFullPath(fn);
            var metadata = new List<Dictionary<string, string>>();
            var duration = GetDuration(fn);
            var NFiles = (int) Math.Ceiling(duration / seconds);

            var oldExtension = "." + fn.Split('.').Last();
            var newExtension = convertToWebm ? ".webm" : oldExtension;

            var cmd = new CMDWithOutput();

            for (var i = 0; i < NFiles; i++)
            {
                var curMetadata = new Dictionary<string, string>();

                var a = i * seconds;
                var b = (i + 1) * seconds;
                var newFn = $"{rootFn}_{i}{newExtension}";

                if (directory != null) newFn = Path.Combine(directory, newFn);
                newFn = Path.GetFullPath(newFn);

                var arguments = $"-loglevel panic -i {fn} -ss {a} -to {b} -c copy";

                if (convertToWebm && oldExtension != ".webm")
                    arguments += " -c:v libvpx -minrate 1M -maxrate 1M -b:v 1M -c:a libvorbis";

                arguments += $" {newFn}";

                var output = cmd.runCMD(ffPath, arguments);

                curMetadata["fn"] = newFn;
                curMetadata["duration"] = Math.Min(b - a, duration - a).ToString();
                metadata.Add(curMetadata);
            }

            return metadata;
        }

        public string ConcatSameCodecs(List<string> fns, string outputFn, string dir = null)
        {
            // https://trac.ffmpeg.org/wiki/Concatenate
            // ffmpeg -f concat -safe 0 -i mylist.txt -c copy output
            var guidFn = $"{Guid.NewGuid()}.txt";
            if (dir != null) guidFn = Path.Combine(dir, guidFn);

            fns = fns.Select(fn => Path.GetFullPath(fn)).ToList();
            guidFn = Path.GetFullPath(guidFn);
            outputFn = Path.GetFullPath(outputFn);

            var cmd = new CMDWithOutput();
            var arguments = $"-f concat -safe 0 -i {guidFn} -c copy {outputFn}";

            // generate mylist.txt content
            /*# this is a comment
            file 'C:\\Users\\arsen\\Desktop\\hb\\hb-operations\\research\\videomerge\\a.webm'
            file 'b.webm'*/
            var content = "";
            foreach (var fn in fns) content += $"file '{fn.Replace(Path.DirectorySeparatorChar.ToString(), @"/")}'\n";

            File.WriteAllText(guidFn, content);
            var res = cmd.runCMD(ffPath, arguments);

            // delete file
            OS.SafeDelete(guidFn);
            return res;
        }

        public string ConcatDifferentCodecs(List<string> fns, string outputFn)
        {
            // https://trac.ffmpeg.org/wiki/Concatenate
            /*ffmpeg -i input1.mp4 -i input2.webm -i input3.mov -filter_complex "[0:v:0][0:a:0][1:v:0][1:a:0][2:v:0][2:a:0]concat=n=3:v=1:a=1[outv][outa]" -map "[outv]" -map "[outa]" output.mkv*/
            var cmd = new CMDWithOutput();
            var arguments = "";

            fns = fns.Select(fn => Path.GetFullPath(fn)).ToList();

            foreach (var fn in fns) arguments += $"-i \"{fn}\" ";

            arguments += "-filter_complex \"";

            for (var i = 0; i < fns.Count; i++) arguments += $"[{i}:v:0][{i}:a:0]";
            arguments += $"concat=n={fns.Count}:v=1:a=1[outv][outa]\" -map \"[outv]\" -map \"[outa]\" {outputFn}";
            return cmd.runCMD(ffPath, arguments);
        }

        /// <summary>
        ///     Cut fragment from blob. Copies all codecs.
        ///     <seealso cref="https://trac.ffmpeg.org/wiki/Seeking" />
        /// </summary>
        /// <param name="fn">Input file name.</param>
        /// <param name="outFn">Output file name.</param>
        /// <param name="sTime">Start of the fragment.</param>
        /// <param name="eTime">End of the fragment.</param>
        /// <param name="outputSeek">Seek fragment in output stream instead of input stream.</param>
        /// <returns>Ffmpeg's output.</returns>
        public string CutBlob(string fn, string outFn, string sTime, string eTime, bool outputSeek = false)
        {
            //https://trac.ffmpeg.org/wiki/Seeking
            //ffmpeg -ss {sTime} -i {fn} -to {eTime} -acodec copy -vcodec copy -avoid_negative_ts 1 {outFn}
            //OR IF outputSeek (more precise, much more slow) :
            //ffmpeg -i {fn} -ss {sTime} -to {eTime} -acodec copy -vcodec copy -avoid_negative_ts 1 {outFn}
            //EXAMPLE:
            //ffmpeg -ss 02:01 -i blob.mkv -to 05:37 -acodec copy -vcodec copy -avoid_negative_ts 1 out.mkv
            var cmd = new CMDWithOutput();
            var args = "";

            if (outputSeek)
                args += $"-i {fn} -ss {sTime} ";
            else
                args += $"-ss {sTime} -i {fn} ";
            args += $"-to {eTime} -acodec copy -vcodec copy -avoid_negative_ts 1 {outFn}";

            return cmd.runCMD(ffPath, args);
        }

        public string SplitToKeyFrames(string fn, string directory = null, string prefix = "keyframe")
        {
            // ffmpeg -i full.mp4 -acodec copy -f segment -vcodec copy -reset_timestamps 1 -map 0 keyframes_mp4/OUTPUT%d.mp4
            fn = Path.GetFullPath(fn);
            var cmd = new CMDWithOutput();

            var ext = Path.GetExtension(fn);

            var newFn = $"{prefix}%d{ext}";
            if (directory != null) newFn = Path.Combine(directory, newFn);

            var arguments = $"-i {fn} -acodec copy -f segment -vcodec copy -reset_timestamps 1 -map 0 {newFn}";
            return cmd.runCMD(ffPath, arguments);
        }
    }
}