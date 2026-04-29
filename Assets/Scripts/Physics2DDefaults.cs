using UnityEngine;

public static class Physics2DDefaults
{
    private static PhysicsMaterial2D stableSurfaceMaterial;

    public static PhysicsMaterial2D StableSurfaceMaterial
    {
        get
        {
            if (stableSurfaceMaterial)
            {
                return stableSurfaceMaterial;
            }

            stableSurfaceMaterial = new PhysicsMaterial2D("Stable 2D Surface")
            {
                friction = 0.7f,
                bounciness = 0f
            };
            stableSurfaceMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            return stableSurfaceMaterial;
        }
    }

    public static void ApplyStableMaterial(Collider2D[] colliders)
    {
        if (colliders == null)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (!collider || collider.isTrigger)
            {
                continue;
            }

            collider.sharedMaterial = StableSurfaceMaterial;
        }
    }
}
