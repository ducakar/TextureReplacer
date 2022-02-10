/*
 * Copyright © 2013-2020 Davorin Učakar
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

using System.Linq;

namespace TextureReplacer
{
    /// <summary>
    /// This class takes cares of EVA Kerbal to assign appropriate suit on creation and manage reflection script.
    /// </summary>
    public class TREvaModule : PartModule
    {
        private Reflections.Script reflectionScript;

        public override void OnStart(StartState state)
        {
            bool useEvaSuit = false;

            ProtoCrewMember kerbal = part.protoModuleCrew.FirstOrDefault();
            if (kerbal != null)
            {
                var kerbalEva = GetComponent<KerbalEVA>();

                useEvaSuit = kerbal.hasHelmetOn || !kerbalEva.CanEVAWithoutHelmet();
                Personaliser.Instance.PersonaliseEva(part, kerbal, useEvaSuit);
            }

            if (Reflections.Instance.ReflectionType == Reflections.Type.Real)
            {
                reflectionScript = new Reflections.Script(part, 1);
                reflectionScript.SetActive(useEvaSuit);
            }
        }

        public void OnHelmetChanged(bool hasHelmet)
        {
            ProtoCrewMember kerbal = part.protoModuleCrew.FirstOrDefault();
            if (kerbal != null)
            {
                Personaliser.Instance.PersonaliseEva(part, kerbal, hasHelmet);
            }

            reflectionScript?.SetActive(hasHelmet);
        }

        public void OnDestroy()
        {
            reflectionScript?.Destroy();
        }
    }
}
