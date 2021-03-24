﻿using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Test
{
    public static class NuberingUtils
    {
        public static IDictionary<int, int> CreateSubstFromString(string subst)
        {
            if (string.IsNullOrEmpty(subst))
            {
                return null;
            }
            return subst
                .Split(' ')
                .Select(s => s.Split('=').Select(i => int.Parse(i)).ToArray())
                .Select(i => new { k = i[0], v = i[1] })
                .ToDictionary(i => i.k, i => i.v);
        }

        public static int GetSubst(IDictionary<int, int> substDictionary, int key)
        {
            if (substDictionary == null)
            {
                return key;
            }

            return substDictionary[key];
        }
    }
}



