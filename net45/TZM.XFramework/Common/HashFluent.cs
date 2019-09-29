//using System.Collections;

//namespace TZM.XFramework
//{
//    public class HashFluent
//    {
//        private int _seed = 17;
//        private int _hashContext;

//        public HashFluent Hash<T>(T obj)
//        {
//            // why 31?
//            // https://computinglife.wordpress.com/2008/11/20/why-do-hash-functions-use-prime-numbers/
//            // shortly, to reduce the conflict of hashing key's distrabution
//            _hashContext = 31 * _seed + ((obj == null) ? -1 : obj.GetHashCode());
//            return this;
//        }

//        public HashFluent Hash(int? value)
//        {
//            _hashContext = 31 * _seed + ((value == null) ? -1 : value.GetHashCode());
//            return this;
//        }

//        public HashFluent Hash(IEnumerable sequence)
//        {
//            if (sequence == null)
//            {
//                _hashContext = 31 * _hashContext + -1;
//            }
//            else
//            {
//                foreach (var element in sequence)
//                {
//                    _hashContext = 31 * _hashContext + ((element == null) ? -1 : element.GetHashCode());
//                }
//            }
//            return this;
//        }

//        public override int GetHashCode()
//        {
//            return _hashContext;
//        }

//        // add more overridings here ..
//        // add value types overridings to avoid boxing which is important
//    }
//}
