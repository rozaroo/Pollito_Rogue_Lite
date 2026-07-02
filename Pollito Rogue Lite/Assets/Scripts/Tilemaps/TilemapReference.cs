using UnityEngine;
using UnityEngine.Tilemaps;
using Enums;

namespace Tilemaps
{
    //Este script requiere que el objeto tenga un componente Tilemap
    [RequireComponent(typeof(Tilemap))]
    public class TilemapReference : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap; //Referencia al componente Tilemap del objeto
        [SerializeField] private CustomTileLayer _layer; //Tipo de capa (definido en un enum externo)
        [SerializeField] private CustomTileAlpha _alpha; //Nivel de transparencia o visibilidad (otro enum externo)
        
        // Propiedades publicas de solo lectura para acceder a las variables privadas
        public CustomTileLayer Layer => _layer; //Devuelve la capa asignada
        public CustomTileAlpha Alpha => _alpha; //Devuelve el estado de alpha asignado
        public Tilemap Tilemap => _tilemap; //Devuelve el tilemap asignado

        private void Awake()
        {
            _tilemap = GetComponent<Tilemap>();
            if (_tilemap == null) Debug.LogError($"GetTilemapManager requires a Tilemap component on {name}");
        }
    }
    //Proposito: Este script guarda una referencia a un Tilemap y a un par de configuraciones personalizadas (Layer y Alpha) que seguramente se usan para
    // identificar que capa de tiles es y que transparencia tiene
    // Uso: Lo pines en cualquier GameObject con un Tilemap. Al arrancar, el Awake() asegura que realmente hay un Tilemap y lo guarda en la variable
    // Acceso: Otros scripts pueden acceder a Layer, Alpha y Tilemap mediante las propiedades publicas
    // Este codigo es mas de organizacion y referencia que de logica pesada
}