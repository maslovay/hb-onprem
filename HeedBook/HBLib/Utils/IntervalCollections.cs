using System;
using System.Collections.Generic;
using System.Linq;
using HBLib.Extensions;

// todo: proper Key func. Maybe round there
namespace HBLib.Utils
{
    public class Interval : List<double>
    {
        public Interval(double a, double b)
        {
            Add(a);
            Add(b);
        }

        public double Length()
        {
            return this[1] - this[0];
        }

        public bool Contains(double? point)
        {
            if (point == null) return false;
            return (point >= this[0]) & (point <= this[1]);
        }

        public string Key()
        {
            return this.JsonPrint();
        }
    }

    public class IntervalCollection : List<Interval>
    {
        public IntervalCollection(params Interval[] intervals)
        {
            AddRange(intervals);
        }

        public bool isEmpty()
        {
            return Count == 0;
        }

        public bool isMonolith()
        {
            return Count == 1;
        }

        public double Length()
        {
            return this.Select(p => p.Length()).Sum();
        }

        public List<string> Keys()
        {
            return this.Select(p => p.Key()).ToList();
        }

        public List<double> GetPoints()
        {
            var res = new List<double>();
            foreach (var x in this) res.AddRange(x);
            return res;
        }

        public string JsonPrint()
        {
            return string.Join("-", this.Select(x => x.JsonPrint()).ToList());
        }

        public static Dictionary<string, IntervalCollection> CalcSets(IntervalCollection a, IntervalCollection b)
        {
            // get union ("a|b"), intersection ("a&b"), minus ("a-b") ("b-a");
            var points = new List<double>();
            points.AddRange(a.GetPoints());
            points.AddRange(b.GetPoints());
            points = points.Distinct().ToList();
            points = points.OrderBy(p => p).ToList();

            var aSplit = a.Split(points);
            var bSplit = b.Split(points);

            var tmp = new List<Interval>();
            tmp.AddRange(aSplit);
            tmp.AddRange(bSplit);
            tmp = tmp.GroupBy(p => p.JsonPrint()).Select(g => g.First()).OrderBy(p => p[0]).ToList();

            var totalSplit = new IntervalCollection();
            totalSplit.AddRange(tmp);

            var union = new IntervalCollection();
            var intersection = new IntervalCollection();
            var minus = new IntervalCollection();

            var aKeys = aSplit.Keys();
            var bKeys = bSplit.Keys();

            foreach (var x in totalSplit)
            {
                var b1 = aKeys.Contains(x.Key());
                var b2 = bKeys.Contains(x.Key());
                if (b1 | b2) union.Add(x);
                if (b1 & b2) intersection.Add(x);
                if (b1 & !b2) minus.Add(x);
            }

            return new Dictionary<string, IntervalCollection>
            {
                {"a | b", union.Glue()},
                {"a & b", intersection.Glue()},
                {"a - b", minus.Glue()}
            };
        }

        public static IntervalCollection operator |(IntervalCollection first, IntervalCollection second)
        {
            return CalcSets(first, second)["a | b"];
        }

        public static IntervalCollection operator &(IntervalCollection first, IntervalCollection second)
        {
            return CalcSets(first, second)["a & b"];
        }

        public static IntervalCollection operator -(IntervalCollection first, IntervalCollection second)
        {
            return CalcSets(first, second)["a - b"];
        }

        public IntervalCollection Split(List<double> points)
        {
            points = points.OrderBy(p => p).ToList();
            if (points.Count == 0) return this;
            var res = new IntervalCollection();
            var curPointInd = 0;
            double? curPoint = points[0];
            foreach (var interval in this)
            {
                double? curBeg = interval[0];

                while (curPoint != null && curPoint <= interval[0])
                {
                    curPointInd += 1;
                    if (curPointInd >= points.Count)
                        curPoint = null;
                    else
                        curPoint = points[curPointInd];
                }

                while (interval.Contains(curPoint))
                {
                    if (curPoint != curBeg) res.Add(new Interval((double) curBeg, (double) curPoint));
                    curBeg = curPoint;
                    curPointInd += 1;

                    if (curPointInd >= points.Count)
                        curPoint = null;
                    else
                        curPoint = points[curPointInd];
                }

                if (interval[1] != curBeg) res.Add(new Interval((double) curBeg, interval[1]));
            }

            return res;
        }

        public IntervalCollection Glue(double threshold = 5.0)
        {
            if (Count == 0) return new IntervalCollection();
            var i = 0;
            var n = Count;
            var res = new IntervalCollection();
            var curInterval = new Interval(this[0][0], this[0][1]);
            while (i < n - 1)
            {
                var nextInterval = this[i + 1];
                if (Math.Abs(nextInterval[0] - curInterval[1]) < threshold)
                {
                    curInterval[1] = nextInterval[1];
                }
                else
                {
                    res.Add(curInterval);
                    curInterval = nextInterval;
                }

                i++;
            }

            res.Add(curInterval);
            return res;
        }

        //public static IntervalCollection Collect(List<Interval> blobs, Interval dialogue) {
        //    var resIC = new IntervalCollection();
        //    var dialogueIC = new IntervalCollection(dialogue);

        //    blobs = blobs.OrderBy(p => p[0]).Reverse().ToList();
        //    foreach (var blob in blobs) {
        //        var blobIC = new IntervalCollection(blob);
        //        if ((blobIC & resIC).Length() > 0) {
        //            throw new Exception("Interception too big");
        //        }

        //        if ((blobIC & dialogueIC).Length() == 0) {
        //            continue;
        //        }

        //        resIC = resIC | blobIC;
        //    }
        //    if (!resIC.isMonolith()) {
        //        throw new Exception("Dialogue is not a monolith and hence cannot be created!");
        //    }

        //    double error1 = (dialogueIC - resIC).Length();
        //    if (error1 >= 0.05) {
        //        throw new Exception("Too much of the dialogue is unfilled");
        //    }

        //    double error2 = (resIC - dialogueIC).Length();
        //    if (error2 >= 0.3) {
        //        throw new Exception("Too much of the non-dialogue video is filled");

        //    }
        //    return resIC;
        //}
    }
}