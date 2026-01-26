
using UnityEngine;

public interface IAccessibleTriplePDController
{
    public void ReloadPDController(Vector3 _pValues, Vector3 _dValues, Vector3 _angleTresholdValues, Transform _referenceTransform = null);

    public void ReloadPDController();

    public void BoostPDValues(Vector3 _pFactorBoost, Vector3 _dFactorBoost);
}
