using UnityEngine;

namespace Utils
{
    static class MonoBehaviourExtensions
    {
        /// <summary>
        /// ATTENTION: You must use a single parametered AnonymousType When Calling this method. Usage: obj.AssertFieldIsSet(new {paramname})
        /// 
        /// Assertion for checking that certain MonoBehaviour successor field is set.
        /// LogError and return false in case if not, otherwise returns true
        /// 
        /// Usage: 
        /// If (!AssertFieldIsSet(new {somePrivateOrPublicField}))
        ///     return;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool AssertFieldIsSet<T>(this MonoBehaviour obj, T item) where T : class
        {
            if (item == null)
            {
                Debug.LogError("Wrong AssertFieldIsSet argument");
                return false;
            }

            var property = typeof(T).GetProperties()[0];
            var value = property.GetValue(item, null);
            if (value != null)
                return true;

            Debug.LogError(string.Format("{0}.{1} is not set", obj.GetType().Name, property.Name));
            return false;
        }
    }
}
