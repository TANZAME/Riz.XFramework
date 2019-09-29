
//using System;
//using System.Linq;
//using System.Text;
//using System.Collections.Generic;
//using System.Security.Cryptography;

//namespace TZM.XFramework
//{

//    public class KetamaNodeLocator
//    {
//        //原文中的JAVA类TreeMap实现了Comparator方法，这里我图省事，直接用了net下的SortedList，其中Comparer接口方法）
//        private SortedList<long, string> ketamaNodes = new SortedList<long, string>();
//        //private HashAlgorithm hashAlg;
//        private int numReps = 160;

//        //此处参数与JAVA版中有区别，因为使用的静态方法，所以不再传递HashAlgorithm alg参数
//        public KetamaNodeLocator(List<string> nodes, int nodeCopies)
//        {
//            ketamaNodes = new SortedList<long, string>();

//            numReps = nodeCopies;
//            //对所有节点，生成nCopies个虚拟结点
//            foreach (string node in nodes)
//            {
//                //每四个虚拟结点为一组
//                for (int i = 0; i < numReps / 4; i++)
//                {
//                    //getKeyForNode方法为这组虚拟结点得到惟一名称 
//                    byte[] digest = HashAlgorithm.ComputeMd5(node + i);
//                    /** Md5是一个16字节长度的数组，将16字节的数组每四个字节一组，分别对应一个虚拟结点，这就是为什么上面把虚拟结点四个划分一组的原因*/
//                    for (int h = 0; h < 4; h++)
//                    {
//                        long m = HashAlgorithm.Hash(digest, h);
//                        ketamaNodes[m] = node;
//                    }
//                }
//            }
//        }

//        public string GetPrimary(string k)
//        {
//            byte[] digest = HashAlgorithm.ComputeMd5(k);
//            string rv = GetNodeForKey(HashAlgorithm.Hash(digest, 0));
//            return rv;
//        }

//        string GetNodeForKey(long hash)
//        {
//            string rv;
//            long key = hash;
//            //如果找到这个节点，直接取节点，返回   
//            if (!ketamaNodes.ContainsKey(key))
//            {
//                //得到大于当前key的那个子Map，然后从中取出第一个key，就是大于且离它最近的那个key 说明详见: http://www.javaeye.com/topic/684087
//                var tailMap = from coll in ketamaNodes
//                              where coll.Key > hash
//                              select new { coll.Key };
//                if (tailMap == null || tailMap.Count() == 0)
//                    key = ketamaNodes.FirstOrDefault().Key;
//                else
//                    key = tailMap.FirstOrDefault().Key;
//            }
//            rv = ketamaNodes[key];
//            return rv;
//        }
//    }

//    public class HashAlgorithm
//    {
//        public static long Hash(byte[] digest, int nTime)
//        {
//            long rv = ((long)(digest[3 + nTime * 4] & 0xFF) << 24)
//                    | ((long)(digest[2 + nTime * 4] & 0xFF) << 16)
//                    | ((long)(digest[1 + nTime * 4] & 0xFF) << 8)
//                    | ((long)digest[0 + nTime * 4] & 0xFF);

//            return rv & 0xffffffffL; /* Truncate to 32-bits */
//        }

//        /**
//         * Get the md5 of the given key.
//         */
//        public static byte[] ComputeMd5(string k)
//        {
//            MD5 md5 = new MD5CryptoServiceProvider();

//            byte[] keyBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(k));
//            md5.Clear();
//            //md5.update(keyBytes);
//            //return md5.digest();
//            return keyBytes;
//        }


//        private static readonly SortedDictionary<ulong, string> _circle = new SortedDictionary<ulong, string>();
//        public static void AddNode(string node, int repeat)
//        {
//            for (int i = 0; i < repeat; i++)
//            {
//                string identifier = node.GetHashCode().ToString() + "-" + i;
//                ulong hashCode = Md5Hash(identifier);
//                _circle.Add(hashCode, node);
//            }
//        }

//        public static ulong Md5Hash(string key)
//        {
//            using (var hash = System.Security.Cryptography.MD5.Create())
//            {
//                byte[] data = hash.ComputeHash(Encoding.UTF8.GetBytes(key));
//                var a = BitConverter.ToUInt64(data, 0);
//                var b = BitConverter.ToUInt64(data, 8);
//                ulong hashCode = a ^ b;
//                return hashCode;
//            }
//        }
//        public static string GetTargetNode(string key)
//        {
//            ulong hash = Md5Hash(key);
//            ulong firstNode = ModifiedBinarySearch(_circle.Keys.ToArray(), hash);
//            return _circle[firstNode];
//        }

//        /// <summary>
//        /// 计算key的数值，得出空间归属。
//        /// </summary>
//        /// <param name="sortedArray"></param>
//        /// <param name="val"></param>
//        /// <returns></returns>
//        public static ulong ModifiedBinarySearch(ulong[] sortedArray, ulong val)
//        {
//            int min = 0;
//            int max = sortedArray.Length - 1;

//            if (val < sortedArray[min] || val > sortedArray[max])
//                return sortedArray[0];

//            while (max - min > 1)
//            {
//                int mid = (max + min) / 2;
//                if (sortedArray[mid] >= val)
//                {
//                    max = mid;
//                }
//                else
//                {
//                    min = mid;
//                }
//            }

//            return sortedArray[max];
//        }
//    }
//}
