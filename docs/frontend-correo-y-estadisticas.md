# Cambios de backend para el frontend

Dos cosas nuevas: el correo de confirmación al crear una reserva y un endpoint de estadísticas para el dashboard de administrador. El resto de la API sigue igual.

## 1. Notificaciones de correo (reservas)

No tienes que enviar ningún campo nuevo.

`POST /api/reservations` sigue recibiendo el mismo cuerpo:

```json
{
  "spaceId": 1,
  "fecha": "2026-10-21",
  "horaInicio": "14:00",
  "horaFin": "16:00"
}
```

El backend toma el nombre y el correo del usuario del token JWT y, si la reserva se guarda, envía solo el correo de confirmación con un adjunto `reserva.ics` (Google Calendar, Outlook y Apple Calendar). La respuesta sigue siendo la reserva creada (`201`), sin datos del correo.

Si el SMTP no está configurado o el envío falla, la reserva igual queda creada. No muestres un error de correo en el formulario: el frontend no se entera de ese fallo.

## 2. Estadísticas del dashboard admin

| | |
|---|---|
| Método | `GET` |
| URL | `/api/admin/statistics` |
| Header | `Authorization: Bearer <token>` |
| Rol | `Admin` |

Sin token, o con un token inválido, responde `401`. Con un usuario autenticado que no es admin (estudiante o docente), responde `403`.

```json
{
  "error": "No tienes permiso para realizar esta acción."
}
```

### Respuesta `200`

```json
{
  "totalReservas": 12,
  "reservasActivas": 7,
  "reservasCanceladas": 3,
  "espacioMasPopular": "Aula 101",
  "reservasPorEspacio": [
    { "nombreEspacio": "Aula 101", "cantidad": 5 },
    { "nombreEspacio": "Laboratorio de física", "cantidad": 4 },
    { "nombreEspacio": "Auditorio", "cantidad": 3 }
  ],
  "reservasPorEstado": [
    { "nombreEstado": "Activa", "cantidad": 7 },
    { "nombreEstado": "Cancelada", "cantidad": 3 },
    { "nombreEstado": "Completada", "cantidad": 2 }
  ]
}
```

Los nombres van en camelCase. `reservasPorEspacio` y `reservasPorEstado` llegan ordenados de mayor a menor cantidad. Si no hay reservas, las listas vienen vacías y `espacioMasPopular` es `"Sin reservas"`.

`reservasActivas` cuenta solo el estado `Activa`. `reservasCanceladas` cuenta solo `Cancelada`. `Completada` entra en `totalReservas` y en `reservasPorEstado`, no en esos dos contadores. Los valores posibles de `nombreEstado` son `"Activa"`, `"Cancelada"` y `"Completada"`. Un estado con cero reservas no aparece en la lista.

### Tipos para TypeScript

```ts
export interface EspacioStat {
  nombreEspacio: string;
  cantidad: number;
}

export interface EstadoStat {
  nombreEstado: "Activa" | "Cancelada" | "Completada";
  cantidad: number;
}

export interface DashboardStats {
  totalReservas: number;
  reservasActivas: number;
  reservasCanceladas: number;
  espacioMasPopular: string;
  reservasPorEspacio: EspacioStat[];
  reservasPorEstado: EstadoStat[];
}
```

Para las gráficas puedes usar `reservasPorEspacio` (barras por espacio) y `reservasPorEstado` (dona o barras por estado) sin transformar los nombres de las propiedades.
