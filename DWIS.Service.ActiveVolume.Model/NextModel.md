# Pit Volume Sensor Fusion Model (UKF-based)

This document specifies a physically consistent, discrete-time, nonlinear state-space model for estimating true pit volume on a drilling rig using heterogeneous sensors with delay, bias, missing data, and disturbances. The model is structured for a UKF with augmented states (biases and slowly varying parameters) and robust measurement updates.

## 0. Notation and conventions

- Sample time: $\Delta t$
- Positive flow convention: positive $Q$ increases downstream inventories (toward pits).
- Pipe velocity sign: $v_s > 0$ means pipe moving down (running in hole). $v_s < 0$ means pipe moving up (pulling out of hole).
- All volumes are surface-referenced unless stated.
- Random processes:
  - Process noise: $w_k$
  - Measurement noise: $\nu_k$

---

## 1. State vector

### 1.1 Inventory (physical) states

- $V^{pit}$: pit volume (target)
- $V^{wb}$: wellbore compressibility storage volume
- $V^{bs}$: bell nipple to shakers holdup
- $V^{sp}$: shakers to pits holdup
- $M^{ann}_s$: cuttings mass inventory in annulus

### 1.2 Slowly varying parameter states

- $C^{eff}$: effective compliance
- $\eta_s$: shaker separation efficiency
- $\beta$: mud retained volume per kg cuttings

### 1.3 Bias states

- $b^{pit}, b^{cor}, b^{pad}, b^{cam}, b^{cor,sol}, b^{SPP}$

---

## 2. Known inputs

- $Q^{pump}$: pump flow
- $ROP$: rate of penetration
- $D_{bit}$: bit diameter
- $v_s$: pipe velocity
- $OD, ID$: pipe diameters
- $Q^{add}$: surface additions

Bit on bottom:

$$
m^{BOB}_k =
\begin{cases}
1 & \text{drilling} \\
0 & \text{off bottom}
\end{cases}
$$

---

## 3. Pipe displacement

$$
Q^{OD}_k = \frac{\pi}{4} OD^2 v_{s,k}
$$

$$
Q^{ID}_k = \gamma \frac{\pi}{4} ID^2 v_{s,k}
$$

$$
Q^{disp}_k = Q^{OD}_k - Q^{ID}_k
$$

---

## 4. Compressibility storage

Introduce net formation gain:

$$
Q^{net}_{k+1} = Q^{net}_k + w^{net}_k
$$

Wellbore storage:

$$
V^{wb}_{k+1} = V^{wb}_k + \Delta t \left( Q^{pump}_k + Q^{net}_k - Q^{bn}_k \right) + w^{wb}_k
$$

---

## 5. Pressure model

$$
P_k = P^{static}_k + \Delta P^{pipe}(Q^{pump}_k) + \Delta P^{ann}(Q^{bn}_k) + \frac{V^{wb}_k}{C^{eff}_k}
$$

$$
\Delta P^{pipe}(Q) = a_p |Q|^{n_p}
$$

$$
\Delta P^{ann}(Q) = a_a |Q|^{n_a}
$$

Measurement:

$$
z^{SPP}_k = P_k + b^{SPP}_k + \nu^{SPP}_k
$$

---

## 6. Flowline transport delays

### Time constant

$$
\tau_i(Q) = \tau_{0,i} + \frac{k_i}{(|Q|+Q_{min})^{p_i}}
$$

$$
\alpha_i(Q) = 1 - \exp\left(-\frac{\Delta t}{\tau_i(Q)}\right)
$$

### Bell nipple to shakers

$$
Q^{sh}_{k+1} = Q^{sh}_k + \alpha_1(Q^{bn}_k)(Q^{bn}_k - Q^{sh}_k) + w^{sh}_k
$$

$$
V^{bs}_{k+1} = V^{bs}_k + \Delta t(Q^{bn}_k - Q^{sh}_k) + w^{bs}_k
$$

### Shakers to pits

$$
Q^{pit,in}_{k+1} = Q^{pit,in}_k + \alpha_2(Q^{sh}_k)(Q^{sh}_k - Q^{pit,in}_k) + w^{pit,in}_k
$$

$$
V^{sp}_{k+1} = V^{sp}_k + \Delta t(Q^{sh}_k - Q^{pit,in}_k) + w^{sp}_k
$$

---

## 7. Cuttings generation

$$
A_b = \frac{\pi}{4} D_{bit}^2
$$

$$
\dot m^{gen}_{s,k} = m^{BOB}_k \rho_s A_b ROP_k
$$

Transport:

$$
\dot m^{out}_{s,k} = \lambda(Q^{bn}_k) M^{ann}_{s,k}
$$

$$
\lambda(Q) = \lambda_0 + c \left(\frac{|Q|}{Q_{ref}}\right)^{p_\lambda}
$$

Inventory:

$$
M^{ann}_{s,k+1} = M^{ann}_{s,k} + \Delta t(\dot m^{gen}_{s,k} - \dot m^{out}_{s,k}) + w^{M}_k
$$

Shaker removal:

$$
\dot m^{rem}_{s,k} = \eta_{s,k} \dot m^{out}_{s,k}
$$

Mud removed with cuttings:

$$
Q^{mud,cut}_k = \beta_k \dot m^{rem}_{s,k}
$$

---

## 8. Pit volume balance

$$
V^{pit}_{k+1} = V^{pit}_k + \Delta t(
Q^{pit,in}_k
- Q^{pump}_k
+ Q^{add}_k
- Q^{mud,cut}_k
- Q^{disp}_k
+ Q^{net}_k
) + w^{pit}_k
$$

---

## 9. Measurement models

Pit sensor:

$$
z^{pit}_k = V^{pit}_k + b^{pit}_k + \nu^{pit}_k
$$

Return flow:

$$
z^{cor}_k = Q^{bn}_k + b^{cor}_k + \nu^{cor}_k
$$

$$
z^{cam}_k = Q^{sh}_k + b^{cam}_k + \nu^{cam}_k
$$

Solids:

$$
z^{cor,sol}_k = \dot m^{out}_{s,k} + b^{cor,sol}_k + \nu^{cor,sol}_k
$$

---

## 10. Parameter evolution

$$
C^{eff}_{k+1} = C^{eff}_k + w^C_k
$$

$$
\eta_{s,k+1} = \eta_{s,k} + w^\eta_k
$$

$$
\beta_{k+1} = \beta_k + w^\beta_k
$$

Biases:

$$
b_{k+1} = b_k + w^b_k
$$

---

## 11. UKF implementation

Augmented state:

$$
x^{aug}_k =
\begin{bmatrix}
x_k \\
w_k
\end{bmatrix}
$$

Measurement:

$$
z_k = h(x_k,u_k) + \nu_k
$$

Robust weighting applied on normalized innovation.

---

## UKF-ready implementation spec (C#-implementation-oriented)

This is a concrete, implementable specification: explicit state ordering, explicit process and measurement functions, sensor-mask measurement assembly, recommended UKF tuning, and a practical treatment of $Q^{bn}$ (Pattern A: make it a state).

---

# 1) State, input, and measurement definitions

## 1.1 State vector ordering

State $x_k \in \mathbb{R}^{17}$ ordered as:

1. $V^{pit}$ (m^3)
2. $V^{wb}$ (m^3)
3. $V^{bs}$ (m^3)
4. $V^{sp}$ (m^3)
5. $Q^{bn}$ (m^3/s)
6. $Q^{sh}$ (m^3/s)
7. $Q^{pit,in}$ (m^3/s)
8. $M^{ann}_s$ (kg)
9. $Q^{net}$ (m^3/s)
10. $\theta_C$
11. $\theta_\eta$
12. $\theta_\beta$
13. $b^{pit}$
14. $b^{SPP}$
15. $b^{cor}$
16. $b^{cam}$
17. $b^{cor,sol}$

### Parameter transforms

$$
C^{eff} = \exp(\theta_C)
$$

$$
\eta_s = \frac{1}{1 + \exp(-\theta_\eta)}
$$

$$
\beta = \exp(\theta_\beta)
$$

---

## 1.2 Inputs

- $Q^{pump}$
- $Q^{add}$
- $ROP$
- $D_{bit}$
- $\rho_s$
- $v_s$
- $OD, ID$
- $m^{BOB}$
- $P^{static}$

Pipe velocity convention:

- $v_s > 0$ running in hole
- $v_s < 0$ pulling out of hole

---

## 1.3 Measurements and mask

Possible sensors:

- Pit volume $z^{pit}$
- Standpipe pressure $z^{SPP}$
- Bell nipple flow $z^{cor}$
- Shaker flow $z^{cam}$
- Solids mass flow $z^{cor,sol}$

Boolean mask:

- `HasPit`
- `HasSPP`
- `HasCor`
- `HasCam`
- `HasCorSol`

---

# 2) Model constants

## 2.1 Flowline delays

$$
\tau_i(Q) = \tau_{0,i} + \frac{k_i}{(|Q| + Q_{min})^{p_i}}
$$

$$
\alpha_i(Q) = 1 - \exp\left(-\frac{\Delta t}{\tau_i(Q)}\right)
$$

## 2.2 Bell nipple flow response

$$
\alpha_{bn} = 1 - \exp\left(-\frac{\Delta t}{\tau_{bn}}\right)
$$

## 2.3 Cuttings transport

$$
\lambda(Q) = \lambda_0 + c_\lambda \left(\frac{|Q|}{Q_{ref}}\right)^{p_\lambda}
$$

## 2.4 Pressure friction

$$
\Delta P^{pipe}(Q) = a_p |Q|^{n_p}
$$

$$
\Delta P^{ann}(Q) = a_a |Q|^{n_a}
$$

---

# 3) Process model

## 3.1 Derived quantities

Bit area:

$$
A_b = \frac{\pi}{4} D_{bit}^2
$$

Cuttings generation:

$$
\dot m^{gen}_s = m^{BOB} \rho_s A_b ROP
$$

Pipe displacement:

$$
Q^{OD} = \frac{\pi}{4} OD^2 v_s
$$

$$
Q^{ID} = \gamma \frac{\pi}{4} ID^2 v_s
$$

$$
Q^{disp} = Q^{OD} - Q^{ID}
$$

Cuttings transport:

$$
\lambda = \lambda_0 + c_\lambda \left(\frac{|Q^{bn}|}{Q_{ref}}\right)^{p_\lambda}
$$

---

## 3.2 State propagation

Net formation gain:

$$
Q^{net}_{k+1} = Q^{net}_k + w^{net}_k
$$

Bell nipple flow:

$$
Q^{bn}_{k+1} = Q^{bn}_k + \alpha_{bn}\left((Q^{pump}_k + Q^{net}_k) - Q^{bn}_k\right) + w^{bn}_k
$$

Compressibility storage:

$$
V^{wb}_{k+1} = V^{wb}_k + \Delta t(Q^{pump}_k + Q^{net}_k - Q^{bn}_k) + w^{wb}_k
$$

Surface transport:

$$
Q^{sh}_{k+1} = Q^{sh}_k + \alpha_1(Q^{bn}_k)(Q^{bn}_k - Q^{sh}_k) + w^{sh}_k
$$

$$
V^{bs}_{k+1} = V^{bs}_k + \Delta t(Q^{bn}_k - Q^{sh}_k) + w^{bs}_k
$$

$$
Q^{pit,in}_{k+1} = Q^{pit,in}_k + \alpha_2(Q^{sh}_k)(Q^{sh}_k - Q^{pit,in}_k) + w^{pit,in}_k
$$

$$
V^{sp}_{k+1} = V^{sp}_k + \Delta t(Q^{sh}_k - Q^{pit,in}_k) + w^{sp}_k
$$

Cuttings inventory:

$$
\dot m^{out}_s = \lambda(Q^{bn}_k) M^{ann}_{s,k}
$$

$$
M^{ann}_{s,k+1} = M^{ann}_{s,k} + \Delta t(\dot m^{gen}_{s,k} - \dot m^{out}_{s,k}) + w^{M}_k
$$

Shaker removal:

$$
\dot m^{rem}_s = \eta_s \dot m^{out}_s
$$

Mud removed:

$$
Q^{mud,cut} = \beta \dot m^{rem}_s
$$

Pit volume:

$$
V^{pit}_{k+1} = V^{pit}_k + \Delta t(
Q^{pit,in}_k - Q^{pump}_k + Q^{add}_k - Q^{mud,cut}_k - Q^{disp}_k + Q^{net}_k
) + w^{pit}_k
$$

Parameters and biases:

$$
\theta_{k+1} = \theta_k + w^\theta_k
$$

$$
b_{k+1} = b_k + w^b_k
$$

---

# 4) Measurement model

Pressure:

$$
\hat P = P^{static}_k + a_p |Q^{pump}|^{n_p} + a_a |Q^{bn}|^{n_a} + \frac{V^{wb}}{C^{eff}}
$$

$$
\hat z^{SPP} = \hat P + b^{SPP}
$$

Pit:

$$
\hat z^{pit} = V^{pit} + b^{pit}
$$

Flow:

$$
\hat z^{cor} = Q^{bn} + b^{cor}
$$

$$
\hat z^{cam} = Q^{sh} + b^{cam}
$$

Solids:

$$
\dot m^{out}_s = \lambda(Q^{bn}) M^{ann}_s
$$

$$
\hat z^{cor,sol} = \dot m^{out}_s + b^{cor,sol}
$$

---

# 5) UKF configuration

Sigma points:

$$
\lambda = \alpha^2(n+\kappa)-n
$$

$$
W^{(0)}_m = \frac{\lambda}{n+\lambda}
$$

$$
W^{(0)}_c = \frac{\lambda}{n+\lambda} + (1-\alpha^2+\beta)
$$

$$
W^{(i)} = \frac{1}{2(n+\lambda)}
$$

Typical choice:

- $\alpha = 10^{-2}$
- $\beta = 2$
- $\kappa = 0$

---

# 6) Robust update (Huber)

Normalized residual:

$$
\tilde r_i = \frac{r_i}{\sqrt{S_{ii}}}
$$

If $|\tilde r_i| > \delta$:

$$
R'_{ii} = R_{ii}\left(\frac{|\tilde r_i|}{\delta}\right)^2
$$

---

# 7) Missing sensor handling

- Build $z$ and $R$ only from available sensors
- Freeze $\theta_\eta$ and $\theta_\beta$ learning if no solids or camera
- Freeze $\theta_C$ learning if no pressure

---

# 8) Mode adjustments (minimal)

- Inflate camera variance during shaker overflow
- Inflate flow sensors during gas
- Inflate pressure during pump off

---

# 9) Implementation outcome

This specification directly supports:

- UKF predict using $f(x,u)$
- UKF update using masked $h(x,u)$
- Online disturbance estimation via $Q^{net}$
- Physically conservative pit volume tracking
