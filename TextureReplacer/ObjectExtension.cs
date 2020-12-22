using UnityEngine;

namespace TextureReplacer
{
  public static class ObjectExtension
  {
    public static bool HasValue(this Object o)
    {
      return (object) o != null;
    }
  }
}
