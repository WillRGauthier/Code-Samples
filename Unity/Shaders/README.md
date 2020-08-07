#Shader Code Explanations

##[Stripe Shader](CutoffWithDiffuse.shader)
This shader combines fresnel/rim lighting with world-position-y selective coloring to create translucent, emissive stripes over a texture.

![Stripe Preview](cutoffDiffuseDemo.png)

##[Hologram Shader](Hologram.shader)
This two-pass shader uses transparency and fresnel/rim lighting to create a hologram effect.

![Hologram Preview](hologramDemo.png)

##[Toon Shader](ToonRampSurface.shader)
This is a toon lighting shader where the view direction is transformed into UV coordinates on a black and white ramp texture.

![Toon Preview](toonRampDemo.png)

##[Plasma Shader](VFPlasma.shader)
This shader varies its color channels over time via a bunch of trig functions to produce a plasma/lava lamp effect.

![Plasma Preview](plasmaDemo.png)