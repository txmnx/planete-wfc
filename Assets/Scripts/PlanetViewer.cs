﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Polish pour controler la planete
public class PlanetViewer : MonoBehaviour
{
    public Transform planet;
    public Transform planetSystem;
    public Camera mainCamera;
    public Texture2D baseCursor;
    public Texture2D dragCursor;

    private Vector3 m_PrevMousePos;
    private bool m_IsMoving = false;
    public float m_Speed = 30f;

    public AnimationCurve animCurve;

    private void Update()
    {
        MovePlanet();
    }

    //Pour faire tourner la planete
    private void MovePlanet()
    {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform == planet) {
                    m_IsMoving = true;
                    Cursor.SetCursor(dragCursor, Vector2.zero, CursorMode.Auto);
                    m_PrevMousePos = Input.mousePosition;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0)) {
            m_IsMoving = false;
            Cursor.SetCursor(baseCursor, Vector2.zero, CursorMode.Auto);
        }

        if (m_IsMoving) {
            Vector3 mouseMove = m_PrevMousePos - Input.mousePosition;

            planetSystem.Rotate(Vector3.right, -mouseMove.y * Time.deltaTime * m_Speed, Space.World);
            planetSystem.Rotate(Vector3.up, mouseMove.x * Time.deltaTime * m_Speed, Space.World);

            m_PrevMousePos = Input.mousePosition;
        }
    }

    public IEnumerator SpawnAnimation(float duration)
    {
        float progress = 0;
        Vector3 baseScale = planet.transform.localScale;

        while (progress < duration) {
            progress += Time.deltaTime;
            float percent = Mathf.Clamp01(progress / duration);
            planet.transform.localScale = baseScale * animCurve.Evaluate(percent);
            yield return null;
        }
    }

    public void AlignOnTile(Tile tile)
    {
        planetSystem.localRotation = Quaternion.Euler(Vector3.zero);
        Vector3 alignedRayTile = tile.transform.position - planet.transform.position;
        planetSystem.localRotation = Quaternion.FromToRotation(alignedRayTile, -planetSystem.forward);
    }

    public void Reset()
    {
        planetSystem.localEulerAngles = Vector3.zero;
    }
}
