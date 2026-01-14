using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	class ExternalHelpers
	{
#if COZY_WEATHER
		public static bool IsCozyWeatherFogEnabled()
		{
			var cozyWeather = DistantLands.Cozy.CozyWeather.instance;
			if (!cozyWeather)
			{
				return false;
			}

			if(!cozyWeather.isActiveAndEnabled)
			{
				return false;
			}

			if (cozyWeather.fogStyle == DistantLands.Cozy.CozyWeather.FogStyle.off)
			{
				return false;
			}

			return true;
		}
#endif

	}
}
