using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class Rule
    {
        public TermList Data = new TermList();

        public Rule(ITerm element)
        {
            Data.Add(element);
        }

        public Rule(params ITerm[] terms)
        {
            foreach (var term in terms)
            {
                Data.Add(term);
            }
        }

        public Rule(TermList expression)
        {
            foreach (var term in expression)
            {
                Data.Add(term);
            }
        }

        //public static Rule operator +(Rule rule1, Rule rule2)
        //{
        //    foreach (var term in rule2.Data)
        //    {
        //        rule1.Data.Add(term);
        //    }
        //    return rule1;
        //}


        public override string ToString()
        {
            return "(" + Data + ")";
        }

        //        try
        //    {
        //    public override string ToString()
        //{


        //public class TermListList : List<TermList>
        //        {
        //            return string.Join("|", from term in this select string.Join("+", term));
        //        }
        //        catch (Exception e)
        //        {
        //            return "(error: " + e.Message + ")";
        //        }
        //    }
        //}
    }


    //public class Rule // : Term
    //{
    //    public TermListList Data;

    //    public Rule()
    //    {
    //        Data = new TermListList();
    //    }

    //    public Rule(ITerm element) : this()
    //    {
    //        Data.Add(new TermList() { element });
    //    }

    //    public Rule(TermList expression) : this()
    //    {
    //        foreach (var term in expression)
    //        {
    //            Data.Add(new TermList() { term });
    //        }
    //    }

    //    //    public override string Name { get; }

    //    public static Rule operator +(Rule rule1, Rule rule2)
    //    {
    //        rule1.Data.AddRange(rule2.Data);
    //        return rule1;
    //    }


    //    public override string ToString()
    //    {
    //        return "(" + Data + ")";
    //    }


    //    public class TermListList : List<TermList>
    //    {
    //        public override string ToString()
    //        {
    //            try
    //            {
    //                return string.Join("|", from term in this select string.Join("+", term));
    //            }
    //            catch (Exception e)
    //            {
    //                return "(error: " + e.Message + ")";
    //            }
    //        }
    //    }
    //}
}