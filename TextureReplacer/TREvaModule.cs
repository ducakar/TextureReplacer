/*
 * Copyright © 2013-2017 Davorin Učakar
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

namespace TextureReplacer
{
  /**
   * This class takes cares of EVA Kerbal to assign appropriate suit on creation, watch for atmospehere charges (to put
   * on spacesuit of it leaves breathable atmosphere) and enables "Toggle EVA Suit" action.
   *
   *
   * It is added to EVA kerbal by Personaliser so it doesn't need to be public.
   */
  public class TREvaModule : PartModule
  {
    Reflections.Script reflectionScript;

    [KSPField(isPersistant = true)]
    bool isInitialised;

    [KSPField(isPersistant = true)]
    bool hasEvaSuit;

    [KSPEvent(guiActive = true, name = "ToggleEvaSuit", guiName = "Toggle EVA Suit")]
    public void ToggleEvaSuit()
    {
      Personaliser personaliser = Personaliser.Instance;

      if (personaliser.PersonaliseEva(part, !hasEvaSuit)) {
        hasEvaSuit = !hasEvaSuit;
        if (reflectionScript != null) {
          reflectionScript.SetActive(hasEvaSuit);
        }
      } else {
        ScreenMessages.PostScreenMessage("No breathable atmosphere", 5.0f, ScreenMessageStyle.UPPER_CENTER);
      }
    }

    public override void OnStart(StartState state)
    {
      Personaliser personaliser = Personaliser.Instance;

      if (!isInitialised) {
        if (!personaliser.IsAtmSuitEnabled) {
          Events["ToggleEvaSuit"].active = false;
          hasEvaSuit = true;
        }
        isInitialised = true;
      }

      if (!personaliser.PersonaliseEva(part, hasEvaSuit)) {
        hasEvaSuit = true;
      }
      if (Reflections.Instance.IsVisorReflectionEnabled &&
          Reflections.Instance.ReflectionType == Reflections.Type.Real) {
        reflectionScript = new Reflections.Script(part, 1);
        reflectionScript.SetActive(hasEvaSuit);
      }
    }

    public void Update()
    {
      Personaliser personaliser = Personaliser.Instance;

      if (!hasEvaSuit && !personaliser.IsAtmBreathable()) {
        personaliser.PersonaliseEva(part, true);
        hasEvaSuit = true;

        if (reflectionScript != null) {
          reflectionScript.SetActive(true);
        }
      }
    }

    public void OnDestroy()
    {
      if (reflectionScript != null) {
        reflectionScript.Destroy();
      }
    }
  }
}
