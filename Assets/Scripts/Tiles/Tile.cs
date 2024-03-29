﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileSide { AB, BC, CA}

//Represente un triangle du pentakis dodecahedre
//C'est une "case" qui hébergera à la fin de l'algorithme un motif particulier (une TileModel)
public class Tile : MonoBehaviour
{
    /*
     * Dans un pentakis dodecahedre haque tile est un triangle isocele tel que :
     *       B 
     *     /  \ 
     *    A -- C
     * avec AB == BC
     */

    public Tile[] neighbours = new Tile[3];
    public TileGenerator tileGenerator;

    public bool isCollapsed = false;

    private TileModel[] m_TileModels;
    private TileModel m_SavedTileModel;

    private float m_SumAllWeights = 0;
    private float m_SumAllWeightsLogWeights = 0;

    private float m_EntropyNoise;


    public void InitTileModels(TileModel[] tileModels, float sumWeights, float sumWeightsLogWeights)
    {
        int cpt = 0;
        m_TileModels = new TileModel[tileModels.Length];
        foreach (TileModel tileModel in tileModels) {
            m_TileModels[cpt++] = Instantiate(tileModel, transform);
        }

        m_SumAllWeights = sumWeights;
        m_SumAllWeightsLogWeights = sumWeightsLogWeights;
        m_EntropyNoise = Random.Range(0.0f, 0.001f);
    }

    //On choisit le modele de la tile
    public void Collapse()
    {
        if (HasNoPossibleTiles()) {
            tileGenerator.OnContradiction();
        }

        int tileModelIndex = GetPossibleTileIndex();
        
        m_SavedTileModel = Instantiate(m_TileModels[tileModelIndex].transform, transform).GetComponent<TileModel>();
        ShowSavedTile();
        
        isCollapsed = true;
        
        for (int i = 0; i < m_TileModels.Length; ++i) {
            if (i != tileModelIndex) {
                m_TileModels[i].isPossible = false;
                m_TileModels[i].name = "=" + i + "=";
            }
        }
        
    }

    private void ShowSavedTile()
    {
        foreach (Transform obj in m_SavedTileModel.GetComponentsInChildren<Transform>()) {
            obj.gameObject.layer = 0;
        }
    }

    private int GetPossibleTileIndex()
    {
        int remaining = Random.Range(0, (int)m_SumAllWeights);
        for (int index = 0; index < m_TileModels.Length; ++index) {
            if (m_TileModels[index].isPossible) {
                if (remaining >= m_TileModels[index].weight) {
                    remaining -= m_TileModels[index].weight;
                }
                else {
                    return index;
                }
            }
        }
        throw new System.ArithmeticException("Erreur : m_SumAllWeights ne reflete pas la somme des probas de toute les tiles possibles");
    }


    public void LaunchPropagation()
    {
        neighbours[(int)TileSide.AB].Propagate(this, TileSide.AB);
        neighbours[(int)TileSide.BC].Propagate(this, TileSide.BC);
        neighbours[(int)TileSide.CA].Propagate(this, TileSide.CA);
    }

    private void Propagate(Tile prev, TileSide side)
    {
        if (isCollapsed) return;

        if (prev.HasNoPossibleTiles() && !prev.isCollapsed) {
            tileGenerator.OnContradiction();
            return;
        }

        bool hasChanged = false;

        //Ici on suit une méthode triviale pour choisir quels sont les TileModel possibles à enlever
        //On regarde pour chaque TileModel de la tile courante si il existe au moins une TileModel de la tile précédente qui puisse s'assembler avec elle
        //Il est sans doute possible de trouver une méthode plus efficace
        foreach ((TileModel possibleTileModel, int id) in new PossibleTileModelIterator(m_TileModels)) {
            bool foundCompatible = false;
            foreach ((TileModel prevPossibleTileModel, int idPrev) in new PossibleTileModelIterator(prev.GetTileModels())) {
                if (prevPossibleTileModel.IsCompatible(possibleTileModel, side)) {
                    foundCompatible = true;
                    break;
                }
            }
            
            if (!foundCompatible) {
                RemovePossibleTileModel(id);
                hasChanged = true;
            }
        }


        //On continue la propagation si on a supprimé au moins une tile possible
        if (hasChanged) {
            tileGenerator.RegisterNewEntropy(this);
            switch (side) {
                case TileSide.AB:
                    neighbours[(int)TileSide.BC].Propagate(this, TileSide.BC);
                    neighbours[(int)TileSide.CA].Propagate(this, TileSide.CA);
                    break;
                case TileSide.BC:
                    neighbours[(int)TileSide.CA].Propagate(this, TileSide.CA);
                    neighbours[(int)TileSide.AB].Propagate(this, TileSide.AB);
                    break;
                case TileSide.CA:
                    neighbours[(int)TileSide.AB].Propagate(this, TileSide.AB);
                    neighbours[(int)TileSide.BC].Propagate(this, TileSide.BC);
                    break;
                default:
                    break;
            }
        }
    }

    private bool HasNoPossibleTiles()
    {
        int cpt = 0;
        foreach ((TileModel possibleTileModel, int id) in new PossibleTileModelIterator(m_TileModels)) {
            cpt++;
        }
        return (cpt < 1);
    }

    private void RemovePossibleTileModel(int index)
    {
        //Debug.Log(transform.name + " - Remove Tile Model =" + index + "=");
        m_TileModels[index].isPossible = false;
        m_TileModels[index].transform.name = "=" + index + "=";
        m_SumAllWeights -= m_TileModels[index].weight;
        m_SumAllWeightsLogWeights -= m_TileModels[index].weight * Mathf.Log(m_TileModels[index].weight, 2);
    }

    public float Entropy()
    {
        return (Mathf.Log(m_SumAllWeights, 2) - (m_SumAllWeightsLogWeights / m_SumAllWeights)) + m_EntropyNoise;
    }

    public void RemoveSavedTileModel()
    {
        if (m_SavedTileModel != null) {
            Destroy(m_SavedTileModel.gameObject);
        }
    }

    public void ResetTile(float sumWeights, float sumWeightsLogWeights)
    {
        RemoveSavedTileModel();
        foreach (TileModel tileModel in m_TileModels) {
            tileModel.isPossible = true;
            tileModel.transform.name = "POSSIBLE";
        }
        isCollapsed = false;
        m_SumAllWeights = sumWeights;
        m_SumAllWeightsLogWeights = sumWeightsLogWeights;
    }

    public static TileSide GetOppositeSide(TileSide side)
    {
        TileSide opposite = TileSide.AB;

        switch (side) {
            case TileSide.AB:
                opposite = TileSide.BC;
                break;
            case TileSide.BC:
                opposite = TileSide.AB;
                break;
            case TileSide.CA:
                opposite = TileSide.CA;
                break;
            default:
                break;
        }

        return opposite;
    }

    public TileModel[] GetTileModels()
    {
        return m_TileModels;
    }
}

//Pour itérer seulement sur les TileModel possibles
public class PossibleTileModelIterator : IEnumerable<(TileModel, int)>
{
    private readonly TileModel[] m_TileModels;
    public PossibleTileModelIterator(TileModel[] tileModels)
    {
        m_TileModels = tileModels;
    }

    public IEnumerator<(TileModel, int)> GetEnumerator()
    {
        for (int i = 0; i < m_TileModels.Length; ++i) {
            if (m_TileModels[i].isPossible) {
                yield return (m_TileModels[i], i);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}