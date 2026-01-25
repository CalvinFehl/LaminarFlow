#ifndef FLUIDSOLVER_COMMON
#define FLUIDSOLVER_COMMON

#define VELOCITY_LAYER1(vel) vel.xy
#define VELOCITY_LAYER2(vel) vel.zw

#define FLUID_LAYER1(fluid) fluid.x
#define FLUID_LAYER2(fluid) fluid.y

//2.1.2. Height Integration Equation 4
float GetFluidHeightDelta(in float2 uv, in float2 velocity, in float2 velocityLB)
{
	float fluidC = SampleFluidHeight(uv, float2( 0, 0)).x;
	float fluidL = SampleFluidHeight(uv, float2(-1, 0)).x;
	float fluidR = SampleFluidHeight(uv, float2( 1, 0)).x;
	float fluidB = SampleFluidHeight(uv, float2( 0,-1)).x;
	float fluidT = SampleFluidHeight(uv, float2( 0, 1)).x;

	float hui = (velocity.x <= 0 ? fluidR : fluidC);
	float huj = (velocity.y <= 0 ? fluidT : fluidC);
	float hvi = (velocityLB.x >= 0 ? fluidL : fluidC);
	float hvj = (velocityLB.y >= 0 ? fluidB : fluidC);

	return ((hui * velocity.x) - (hvi * velocityLB.x)) * CELLSIZE_RCP_LAYER1.x + ((huj * velocity.y) - (hvj * velocityLB.y)) * CELLSIZE_RCP_LAYER1.y;
}

float2 GetFluidHeightDelta(in float2 uv)
{
	float2 heightDelta = 0;
	float2 fluidC = SampleFluidHeight(uv, float2( 0, 0));
	float2 fluidL = SampleFluidHeight(uv, float2(-1, 0));
	float2 fluidR = SampleFluidHeight(uv, float2( 1, 0));
	float2 fluidB = SampleFluidHeight(uv, float2( 0,-1));
	float2 fluidT = SampleFluidHeight(uv, float2( 0, 1));

	float4 velocityC = SampleVelocity(uv, float2( 0, 0));
	float4 velocityL = SampleVelocity(uv, float2(-1, 0));
	float4 velocityB = SampleVelocity(uv, float2( 0,-1));

	//2.1.5. Stability Enhancements
    float2 avgMaxHeight = (fluidL + 
                          fluidR + 
                          fluidB + 
                          fluidT) / 4.0;

	float beta = 2;
    float2 hadj = max(0, avgMaxHeight - (beta * (_CellSize.xz / (ACCELERATION_LAYER1 * _FluidSimDeltaTime)) )) * any(fluidC);

	// _FluidDeltaMax for stability on deep beter.
	float2 velocityLB = float2(velocityL.x, velocityB.y);
	float hui = (velocityC.x <= 0 ? FLUID_LAYER1(fluidR) : FLUID_LAYER1(fluidC)) - hadj.x;
	float huj = (velocityC.y <= 0 ? FLUID_LAYER1(fluidT) : FLUID_LAYER1(fluidC)) - hadj.x;
	float hvi = (velocityLB.x >= 0 ? FLUID_LAYER1(fluidL) : FLUID_LAYER1(fluidC)) - hadj.x;
	float hvj = (velocityLB.y >= 0 ? FLUID_LAYER1(fluidB) : FLUID_LAYER1(fluidC)) - hadj.x;

	heightDelta.x = ((hui * velocityC.x) - (hvi * velocityLB.x)) * CELLSIZE_RCP_LAYER1.x + ((huj * velocityC.y) - (hvj * velocityLB.y)) * CELLSIZE_RCP_LAYER1.y;

	#if FLUID_MULTILAYER
		velocityLB = float2(velocityL.z, velocityB.w);
		hui = (velocityC.z <= 0 ? FLUID_LAYER2(fluidR) : FLUID_LAYER2(fluidC))- hadj.y;
		huj = (velocityC.w <= 0 ? FLUID_LAYER2(fluidT) : FLUID_LAYER2(fluidC))- hadj.y;
		hvi = (velocityLB.x >= 0 ? FLUID_LAYER2(fluidL) : FLUID_LAYER2(fluidC))- hadj.y;
		hvj = (velocityLB.y >= 0 ? FLUID_LAYER2(fluidB) : FLUID_LAYER2(fluidC))- hadj.y;
		heightDelta.y = ((hui * velocityC.z) - (hvi * velocityLB.x)) * CELLSIZE_RCP_LAYER2.x + ((huj * velocityC.w) - (hvj * velocityLB.y)) * CELLSIZE_RCP_LAYER2.y;
	#endif
	return heightDelta;
}

//2.1.1. Velocity Advection SemiLagrangian
float2 SampleVelocitySemiLagrangian(in float2 uv, in float2 offset)
{
	float2 vel = SampleVelocity(uv, offset).xy;
	float2 advectedUV = uv - vel * _FluidSimDeltaTime * _VelocityField_TexelSize.xy;
	float2 advectedVel = SampleVelocity(advectedUV, offset).xy;
	return advectedVel;
}

//2.1.1. Velocity Advection MacCormack
float4 SampleVelocityMacCormack(in float2 uv, in float2 offset)
{
	float4 velocity = SampleVelocity(uv, offset);
	float4 advectedVelocity = velocity;

	{
		float2 forwardCoord = uv - _FluidSimDeltaTime * VELOCITY_LAYER1(velocity) * _VelocityField_TexelSize.xy * CELLSIZE_RCP_LAYER1;
		float2 forwardVelocity = VELOCITY_LAYER1(SampleVelocity(forwardCoord, offset));

		float2 backwardCoord = uv + _FluidSimDeltaTime * forwardVelocity * _VelocityField_TexelSize.xy * CELLSIZE_RCP_LAYER1;
		float2 backwardVelocity = VELOCITY_LAYER1(SampleVelocity(backwardCoord, offset));

		VELOCITY_LAYER1(advectedVelocity) = 0.5 * (VELOCITY_LAYER1(velocity) + backwardVelocity);
	}

#if FLUID_MULTILAYER
	{
		float2 forwardCoord = uv - _FluidSimDeltaTime * VELOCITY_LAYER2(velocity) * _VelocityField_TexelSize.xy * CELLSIZE_RCP_LAYER2;
		float2 forwardVelocity = VELOCITY_LAYER2(SampleVelocity(forwardCoord, offset));

		float2 backwardCoord = uv + _FluidSimDeltaTime * forwardVelocity * _VelocityField_TexelSize.xy * CELLSIZE_RCP_LAYER2;
		float2 backwardVelocity = VELOCITY_LAYER2(SampleVelocity(backwardCoord, offset));

		VELOCITY_LAYER2(advectedVelocity) = 0.5 * (VELOCITY_LAYER2(velocity) + backwardVelocity);
	}
#endif

	return advectedVelocity;
}

#endif//FLUIDSOLVER_COMMON