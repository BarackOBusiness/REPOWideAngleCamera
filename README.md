# REPO Wide Angle Camera Mod
Implements stereographic projection for REPO to allow for much greater fields of view with more pleasant distortion profiles.\
Unlike my corresponding White Knuckle mod, this is not a testbed for projection techniques or optimizations, it is an opinionated implementation which does little other than it says.
![](https://github.com/barackobusiness/repowideanglecamera/blob/master/assets/gallery/preview.avif?raw=true)

## Features
- Very wide FOV (in horizontal degrees)
- Toggleable backface rendering for performance adjustment at the cost of maximum achievable FOV
- Equisolid projection for crystal ball
- Compatible with [FovUpdate](https://thunderstore.io/c/repo/p/darmuh/FovUpdate) for ultrawide compatibility (FOV config from that mod will not apply obviously)

## Gallery
TODO

## More information
The default perspective projection in the game applies a vertical field of view of 70, whereas this mod takes horizontal field of view as its configurable parameter.
To compare fairly between the two, you have to derive the normal camera's horizontal field of view and match them,
or you may find a horizontal field of view for the stereographic camera that matches the vertical field of view of the normal camera, and compare those,
regardless, the field of view of both axes cannot be the same at once (unless you have a 1:1 monitor), as the projection techniques are different.\
To that end, I've made [this calculator](https://www.desmos.com/calculator/awebrw5jve) to substitute your display parameters and get corresponding numbers for comparison.
