using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Presents Core weapon-anchor events with the equipped weapon and retained effects.
/// </summary>
public sealed class CoreWeaponAnchorPresenter
{
    private const float PlantWeaponStartHeight = 4f;
    private const float PlantEffectStartHeight = 2f;
    private const float PushImpactDelay = 0.15f;
    private const float SwapAnimationLockDuration = 0.5f;

    private readonly GamePlayer_Controller playerController;
    private readonly CameraFollow cameraFollow;

    private GameObject anchorWeaponView;
    private GameObject anchorEffectView;
    private GameObject heldWeaponView;
    private WeaponDataAsset weaponData;
    private SkinDataAsset skinData;
    private bool missingWeaponReported;

    /// <summary>
    /// Creates a presenter for one retained player view and gameplay camera.
    /// </summary>
    public CoreWeaponAnchorPresenter(
        GamePlayer_Controller playerController,
        CameraFollow cameraFollow)
    {
        // Dependencies are views only and never participate in Core resolution.
        this.playerController = playerController;
        this.cameraFollow = cameraFollow;
    }

    #region Presentation Commands

    /// <summary>
    /// Plays the original planting animation while the equipped weapon falls into place.
    /// </summary>
    public IEnumerator PresentPlant(Vector3 anchorPosition, float duration)
    {
        // Core has already accepted the anchor position before presentation begins.
        if (!EnsureAnchorViews())
            yield break;

        playerController.Play("PlantSword");
        yield return AnimateAnchorFall(anchorPosition, duration);
    }

    /// <summary>
    /// Flips the player toward the planted weapon and moves the weapon to the departure cell.
    /// </summary>
    public IEnumerator PresentSwap(
        Vector3 playerStartPosition,
        Vector3 playerEndPosition,
        float swapDuration,
        float anchorFallDuration)
    {
        // The player travels to the old anchor while the weapon takes the departure cell.
        if (!EnsureAnchorViews())
            yield break;

        // The Win clip is the retained forward-flip animation used by the original mechanic.
        playerController.Play(
            "Win",
            true,
            SwapAnimationLockDuration,
            lockedWaitEnd: false);
        yield return AnimatePlayerFlip(
            playerStartPosition,
            playerEndPosition,
            swapDuration);
        SpawnTranspositionEffect(playerEndPosition);
        ShakeCamera();
        yield return AnimateAnchorFall(
            playerStartPosition,
            anchorFallDuration);
    }

    /// <summary>
    /// Plays the shield strike and moves the planted weapon to its resolved destination.
    /// </summary>
    public IEnumerator PresentPush(
        Vector3 anchorStartPosition,
        Vector3 anchorEndPosition,
        float duration,
        GameObject impactPrefab)
    {
        // The event endpoints decide whether the strike succeeds or hits an obstruction.
        if (!EnsureAnchorViews())
            yield break;

        SetAnchorPose(anchorStartPosition);
        if (anchorStartPosition == anchorEndPosition)
        {
            playerController.Play("StrikeShieldHit");
            yield break;
        }

        playerController.Play("StrikeShield");
        yield return new WaitForSeconds(PushImpactDelay);
        SpawnPushImpact(impactPrefab);
        yield return AnimateAnchorSlide(anchorEndPosition, duration);
    }

    /// <summary>
    /// Synchronizes the weapon view to an already resolved anchor position.
    /// </summary>
    public void Synchronize(Vector3 anchorPosition)
    {
        // Direct synchronization covers map restoration without replaying an action.
        if (!EnsureAnchorViews())
            return;

        SetAnchorPose(anchorPosition);
    }

    #endregion

    #region View Lifecycle

    /// <summary>
    /// Releases the planted weapon views and restores the equipped weapon in the player's hand.
    /// </summary>
    public void Clear()
    {
        // Both spawned objects are presentation-owned and safe to release together.
        if (anchorWeaponView != null)
            Object.Destroy(anchorWeaponView);
        if (anchorEffectView != null)
            Object.Destroy(anchorEffectView);

        anchorWeaponView = null;
        anchorEffectView = null;
        weaponData = null;
        skinData = null;
        missingWeaponReported = false;
        RestoreHeldWeapon();
    }

    /// <summary>
    /// Creates the planted weapon and persistent ground effect from the equipped cosmetic.
    /// </summary>
    private bool EnsureAnchorViews()
    {
        // Reuse one view for planting, swapping, pushing, and state synchronization.
        if (anchorWeaponView != null)
        {
            HideHeldWeapon();
            return true;
        }

        if (playerController == null ||
            playerController.PlayerCharacter == null ||
            playerController.PlayerCharacter.CurrentPlayerSave == null ||
            GameContext.Databases == null ||
            GameContext.Databases.SkinsData == null ||
            GameContext.Databases.WeaponData == null)
        {
            ReportMissingWeapon(
                "The weapon anchor cannot be presented because its player or databases are unavailable.");
            return false;
        }

        weaponData = playerController.PlayerCharacter.GetCurrentWeaponData();
        if (weaponData == null)
        {
            ReportMissingWeapon(
                "The weapon anchor cannot be presented because no weapon is equipped.");
            return false;
        }

        skinData = GameContext.Databases.SkinsData.GetSkin(
            weaponData.Type,
            weaponData.SkinID);
        var weaponPrefab = GameContext.Databases.SkinsData.GetPrefab(
            weaponData.Type,
            weaponData.SkinID);
        if (skinData == null || weaponPrefab == null)
        {
            ReportMissingWeapon(
                "The equipped weapon is missing its retained skin data or prefab.");
            return false;
        }

        anchorWeaponView = Object.Instantiate(weaponPrefab);
        anchorWeaponView.name = "Weapon Anchor - " + weaponPrefab.name;
        anchorWeaponView.transform.localScale = weaponData.Scale / 6f;
        anchorEffectView = GameContext.Databases.WeaponData.GetPlantedVFX(
            weaponData.Type,
            weaponData.SkinID);
        heldWeaponView =
            playerController.PlayerCharacter.GetInstantiatedWeaponGameObject();
        HideHeldWeapon();
        return true;
    }

    /// <summary>
    /// Hides the equipped hand-held weapon while its planted copy is active.
    /// </summary>
    private void HideHeldWeapon()
    {
        // Resolve the current instance lazily because map setup can rebuild the character skin.
        if (heldWeaponView == null &&
            playerController != null &&
            playerController.PlayerCharacter != null)
        {
            heldWeaponView =
                playerController.PlayerCharacter.GetInstantiatedWeaponGameObject();
        }

        if (heldWeaponView != null)
            heldWeaponView.SetActive(false);
    }

    /// <summary>
    /// Restores the equipped hand-held weapon after the anchor disappears.
    /// </summary>
    private void RestoreHeldWeapon()
    {
        // A missing cached reference can be recovered from the current character presentation.
        if (heldWeaponView == null &&
            playerController != null &&
            playerController.PlayerCharacter != null)
        {
            heldWeaponView =
                playerController.PlayerCharacter.GetInstantiatedWeaponGameObject();
        }

        if (heldWeaponView != null)
            heldWeaponView.SetActive(true);
        heldWeaponView = null;
    }

    /// <summary>
    /// Reports an unavailable equipped weapon once until the presenter is cleared.
    /// </summary>
    private void ReportMissingWeapon(string message)
    {
        // Avoid flooding the console when synchronization retries on subsequent turns.
        if (missingWeaponReported)
            return;

        missingWeaponReported = true;
        Debug.LogError(message);
    }

    #endregion

    #region Animation Helpers

    /// <summary>
    /// Drops the planted weapon and its ground effect from their original authored heights.
    /// </summary>
    private IEnumerator AnimateAnchorFall(Vector3 cellPosition, float duration)
    {
        // Weapon and ground effect land together, with the lighter effect arriving first.
        var weaponEndPosition = GetWeaponPosition(cellPosition);
        var effectEndPosition = GetEffectPosition(cellPosition);
        var weaponStartPosition =
            weaponEndPosition + Vector3.up * PlantWeaponStartHeight;
        var effectStartPosition =
            effectEndPosition + Vector3.up * PlantEffectStartHeight;
        var safeDuration = Mathf.Max(0.0001f, duration);
        var effectDuration = safeDuration * 0.5f;
        var elapsed = 0f;

        anchorWeaponView.transform.position = weaponStartPosition;
        if (anchorEffectView != null)
            anchorEffectView.transform.position = effectStartPosition;

        while (elapsed < safeDuration)
        {
            anchorWeaponView.transform.position = Vector3.Lerp(
                weaponStartPosition,
                weaponEndPosition,
                elapsed / safeDuration);
            if (anchorEffectView != null)
            {
                anchorEffectView.transform.position = Vector3.Lerp(
                    effectStartPosition,
                    effectEndPosition,
                    Mathf.Clamp01(elapsed / effectDuration));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetAnchorPose(cellPosition);
    }

    /// <summary>
    /// Moves the player along the retained sword-switch arc while facing the destination.
    /// </summary>
    private IEnumerator AnimatePlayerFlip(
        Vector3 startPosition,
        Vector3 endPosition,
        float duration)
    {
        // Facing converges during the first half so the flip visibly follows its trajectory.
        var safeDuration = Mathf.Max(0.0001f, duration);
        var startRotation = playerController.transform.rotation;
        var travelDirection = endPosition - startPosition;
        var endRotation = travelDirection.sqrMagnitude > Mathf.Epsilon
            ? Quaternion.LookRotation(travelDirection, Vector3.up)
            : startRotation;
        var elapsed = 0f;

        playerController.transform.position = startPosition;
        while (elapsed < safeDuration)
        {
            var progress = elapsed / safeDuration;
            var position = Vector3.Lerp(startPosition, endPosition, progress);
            position.y = EvaluateSwapHeight(progress);
            playerController.transform.position = position;
            playerController.transform.rotation = Quaternion.Slerp(
                startRotation,
                endRotation,
                Mathf.Clamp01(progress * 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerController.transform.position = endPosition;
        playerController.transform.rotation = endRotation;
    }

    /// <summary>
    /// Slides the planted weapon and its persistent effect between resolved cells.
    /// </summary>
    private IEnumerator AnimateAnchorSlide(
        Vector3 endCellPosition,
        float duration)
    {
        // Preserve the authored vertical offsets while interpolating both retained views.
        var weaponStartPosition = anchorWeaponView.transform.position;
        var weaponEndPosition = GetWeaponPosition(endCellPosition);
        var effectStartPosition = anchorEffectView == null
            ? Vector3.zero
            : anchorEffectView.transform.position;
        var effectEndPosition = GetEffectPosition(endCellPosition);
        var safeDuration = Mathf.Max(0.0001f, duration);
        var elapsed = 0f;

        while (elapsed < safeDuration)
        {
            var progress = elapsed / safeDuration;
            anchorWeaponView.transform.position = Vector3.Lerp(
                weaponStartPosition,
                weaponEndPosition,
                progress);
            if (anchorEffectView != null)
            {
                anchorEffectView.transform.position = Vector3.Lerp(
                    effectStartPosition,
                    effectEndPosition,
                    progress);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetAnchorPose(endCellPosition);
    }

    /// <summary>
    /// Applies the final authored transform to the planted weapon and its ground effect.
    /// </summary>
    private void SetAnchorPose(Vector3 cellPosition)
    {
        // Cosmetic metadata reproduces the exact planted transform for every weapon skin.
        anchorWeaponView.transform.position = GetWeaponPosition(cellPosition);
        anchorWeaponView.transform.localRotation =
            Quaternion.Euler(weaponData.Rotation) * skinData.localRot;
        anchorWeaponView.transform.localScale = weaponData.Scale / 6f;
        if (anchorEffectView != null)
            anchorEffectView.transform.position = GetEffectPosition(cellPosition);
    }

    /// <summary>
    /// Calculates the planted weapon's authored world position for one grid cell.
    /// </summary>
    private Vector3 GetWeaponPosition(Vector3 cellPosition)
    {
        // The historical data stores planted height at six times the runtime scale.
        return cellPosition +
               Vector3.up * (weaponData.YPos / 6f) +
               skinData.localPos;
    }

    /// <summary>
    /// Calculates the persistent ground effect position for one grid cell.
    /// </summary>
    private static Vector3 GetEffectPosition(Vector3 cellPosition)
    {
        // A slight lift prevents the ground particles from z-fighting with the floor.
        return cellPosition + Vector3.up * 0.025f;
    }

    /// <summary>
    /// Evaluates the original configurable flip arc at normalized progress.
    /// </summary>
    private float EvaluateSwapHeight(float progress)
    {
        // The parabola is only a safe fallback for a missing authored curve.
        var curve = playerController.SwitchSwordCurve;
        var normalizedHeight = curve == null || curve.length == 0
            ? 4f * progress * (1f - progress)
            : curve.Evaluate(progress);
        return normalizedHeight * playerController.SwitchSwordJumpY;
    }

    /// <summary>
    /// Spawns the original strike effect where the planted weapon begins moving.
    /// </summary>
    private void SpawnPushImpact(GameObject impactPrefab)
    {
        // Impact origin follows the visible weapon rather than recalculating gameplay contact.
        if (impactPrefab == null)
        {
            Debug.LogError("The weapon-anchor push impact prefab is not assigned.");
            return;
        }

        var impact = Object.Instantiate(impactPrefab);
        impact.transform.position = anchorWeaponView.transform.position;
    }

    /// <summary>
    /// Spawns the weapon-colored transposition effect at the player's landing cell.
    /// </summary>
    private void SpawnTranspositionEffect(Vector3 playerPosition)
    {
        // WeaponData applies the equipped skin's authored colors to this effect.
        var effect =
            GameContext.Databases.WeaponData.GetTranspositionWeaponVFX(
                weaponData.Type,
                weaponData.SkinID);
        if (effect != null)
            effect.transform.position = playerPosition;
    }

    /// <summary>
    /// Replays the short camera rotation shake used by the original sword swap.
    /// </summary>
    private void ShakeCamera()
    {
        // Missing cameras simply omit feedback without affecting the resolved swap.
        if (cameraFollow == null)
            return;

        var gameplayCamera = cameraFollow.GetComponent<Camera>();
        if (gameplayCamera != null)
            gameplayCamera.DOShakeRotation(0.25f, 10f, 2, 2f);
    }

    #endregion
}
