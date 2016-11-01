﻿/*
 * Copyright © 2013-2016 Davorin Učakar, RangeMachine
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

using UnityEngine;

namespace TextureReplacer
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class TRActivator : MonoBehaviour
    {
        /**
         * Reflection updater. We don't want this to run every frame unless real reflections are enabled
         * so it's wrapped inside another component and enabled only when needed.
         */

        private class TRReflectionUpdater : MonoBehaviour
        {
            public void Update()
            {
                Reflections.Script.updateScripts();
            }
        }

        private bool hasFlightHandlers = false;
        private TRReflectionUpdater reflectionUpdater = null;

        public void Start()
        {
            if (!TextureReplacer.isLoaded)
                return;

            Replacer.instance.beginScene();

            if (HighLogic.LoadedSceneIsFlight)
            {
                Replacer.instance.beginFlight();
                Personaliser.instance.beginFlight();

                hasFlightHandlers = true;
            }

            if ((HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
                && Reflections.instance.reflectionType == Reflections.Type.REAL)
            {
                reflectionUpdater = gameObject.AddComponent<TRReflectionUpdater>();
            }
        }

        public void OnDestroy()
        {
            if (hasFlightHandlers)
            {
                Replacer.instance.endFlight();
                Personaliser.instance.endFlight();
            }

            if (reflectionUpdater != null)
                Destroy(reflectionUpdater);
        }
    }
}