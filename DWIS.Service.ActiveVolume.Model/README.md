# Three-State EKF for Pit Volume Fusion (Return Measured as Proportion)

This document describes a minimal three-state Extended Kalman Filter (EKF) to fuse:

- (a) direct pit volume measurement  
- (b) flow derived volume change from inlet flow, return proportion, and cuttings removal

Key constraint: the return signal is NOT volumetric. It is a proportion of an unknown capacity that must be estimated online.

All equations use discrete time index k and sampling interval dt (seconds).

---

## 1) Signals and conventions

Measured signals:

- $V_m[k]$ : measured pit volume [m^3]
- $q_{in}[k]$ : flow pumped into well from pits [m^3/s]
- $q_{cut}[k]$ : cuttings removed at shakers [m^3/s]
- $r_{ret}[k]$ : measured return proportion (dimensionless)

Unknown, time-varying parameter:

- $C[k]$ : return flow capacity scale [m^3/s]

Return flow reconstruction:

$$
q_{ret}[k] = C[k] \, r_{ret}[k]
$$

Net inflow to pits (positive increases volume):

$$
q_{\Delta}[k] = C[k] r_{ret}[k] - q_{cut}[k] - q_{in}[k]
$$

---

## 2) State definition (3 states)

Estimate pit volume, net-flow bias, and return capacity scale:

$$
x[k] = \begin{bmatrix} V[k] \\\\ b[k] \\\\ C[k] \end{bmatrix}
$$

Where:

- $V[k]$ = true pit volume [m^3]
- $b[k]$ = slowly varying bias in net flow [m^3/s]
- $C[k]$ = return capacity scale [m^3/s]

---

## 3) Nonlinear process model

Process equations:

$$
V[k] = V[k-1] + dt \left(C[k-1] r_{ret}[k] - q_{cut}[k] - q_{in}[k] + b[k-1]\right) + w_V[k]
$$

$$
b[k] = b[k-1] + w_b[k]
$$

$$
C[k] = C[k-1] + w_C[k]
$$

Compact nonlinear form:

$$
x[k] = f(x[k-1], u[k]) + w[k]
$$

with input vector

$$
u[k] = \begin{bmatrix} r_{ret}[k] \\\\ q_{cut}[k] \\\\ q_{in}[k] \end{bmatrix}
$$

and process noise

$$
w[k] = \begin{bmatrix} w_V[k] \\\\ w_b[k] \\\\ w_C[k] \end{bmatrix}, \quad
w[k] \sim \mathcal{N}(0,Q[k])
$$

---

## 4) Measurement model (direct pit volume)

$$
z[k] = V_m[k] = H.x[k] + v[k] = \begin{bmatrix} 1 & 0 & 0 \end{bmatrix} x[k] + v[k]
$$

$$
H = \begin{bmatrix} 1 & 0 & 0 \end{bmatrix}
$$

$$
v[k] \sim \mathcal{N}(0,R)
$$

---

## 5) EKF linearization (Jacobian)

The EKF uses the Jacobian of the process model with respect to the state.

Define the predicted mean before update:

$$
\hat{x}^-_k = f(\hat{x}_{k-1}, u[k])
$$

### 5.1 State transition Jacobian $F_k$

Let $\hat{b}_{k-1}$ and $\hat{C}_{k-1}$ be the previous posterior estimates.

The Jacobian of $f$ w.r.t. $x$ evaluated at $\hat{x}_{k-1}$ is:

$$
F_k = \frac{\partial f}{\partial x}\Big|_{\hat{x}_{k-1}} =
\begin{bmatrix}
1 & dt & dt \, r_{ret}[k] \\\\
0 & 1  & 0 \\\\
0 & 0  & 1
\end{bmatrix}
$$

Notes:
- The nonlinearity is the product $C r_{ret}$; its derivative w.r.t. C is $dt \, r_{ret}[k]$.

---

## 6) Noise covariances

### 6.1 Measurement noise R (pit sensor)

Assume pit volume noise is white and unbiased:

$$
R = \sigma_V^2
$$

Estimate from quiet periods:

$$
\sigma_V \approx \frac{std(V_m[k] - V_m[k-1])}{\sqrt{2}}
$$

### 6.2 Process noise Q[k]

A simple diagonal choice is:

$$
Q[k] =
\begin{bmatrix}
Q_V[k] & 0 & 0 \\\\
0 & Q_b & 0 \\\\
0 & 0 & Q_C
\end{bmatrix}
$$

Interpretation:
- $Q_V[k]$ covers unmodeled volume dynamics and fast mismatch not captured by $b$ or $C$
- $Q_b$ sets how fast the bias can drift (flowmeter drift and general mismatch)
- $Q_C$ sets how fast the capacity scale can drift (slow recalibration online)

Practical starting point:
- Set $Q_b$ and $Q_C$ small (slow random walks)
- Increase $Q_V[k]$ (or add a constant $Q_{model}$) until innovations are consistent (see NIS below)

Optional inclusion of input noise into $Q_V[k]$ by linearization:
If you have white noise variances $\sigma_r^2$, $\sigma_{cut}^2$, $\sigma_{in}^2$,
a practical approximation is:

$$
\sigma_{q_\Delta}^2[k] \approx (\hat{C}_{k-1}^2 \, \sigma_r^2) + \sigma_{cut}^2 + \sigma_{in}^2
$$

$$
Q_V[k] = dt^2 \, \sigma_{q_\Delta}^2[k] + Q_{model}
$$

---

## 7) EKF recursion (fusion of sources a and b)

Maintain posterior estimate $\hat{x}_k$ and covariance $P_k$.

### 7.1 Prediction step

Compute predicted state:

$$
\hat{V}^-_k = \hat{V}_{k-1} + dt\left(\hat{C}_{k-1} r_{ret}[k] - q_{cut}[k] - q_{in}[k] + \hat{b}_{k-1}\right)
$$

$$
\hat{b}^-_k = \hat{b}_{k-1}
$$

$$
\hat{C}^-_k = \hat{C}_{k-1}
$$

Stacked:

$$
\hat{x}^-_k = \begin{bmatrix} \hat{V}^-_k \\\\ \hat{b}^-_k \\\\ \hat{C}^-_k \end{bmatrix}
$$

Covariance prediction:

$$
P^-_k = F_k P_{k-1} F_k^T + Q[k]
$$

### 7.2 Update step (pit volume measurement)

Innovation:

$$
\nu_k = z[k] - H \hat{x}^-_k = V_m[k] - \hat{V}^-_k
$$

Innovation covariance:

$$
S_k = H P^-_k H^T + R
$$

Kalman gain:

$$
K_k = P^-_k H^T S_k^{-1}
$$

State update:

$$
\hat{x}_k = \hat{x}^-_k + K_k \nu_k
$$

Covariance update:

$$
P_k = (I - K_k H) P^-_k
$$

---

## 8) Outlier rejection (recommended)

Normalized innovation squared (1D):

$$
NIS_k = \frac{\nu_k^2}{S_k}
$$

If $NIS_k > \gamma$ (typical 9 to 25), skip the update step and keep prediction.

---

## 9) Outputs

Fused pit volume:

$$
\hat{V}[k] = \hat{x}_k(1)
$$

Estimated net-flow bias:

$$
\hat{b}[k] = \hat{x}_k(2)
$$

Estimated return capacity scale:

$$
\hat{C}[k] = \hat{x}_k(3)
$$

Uncertainty (1 sigma):

$$
\sigma_{\hat{V}}[k] = \sqrt{P_k(1,1)}
$$

---

## 10) Practical tuning notes

1) Set R from quiet pit periods.

2) Initialize:
- $\hat{V}_0 = V_m[0]$
- $\hat{b}_0 = 0$
- $\hat{C}_0$ from a rough manual guess or a short initial calibration window
- Choose a relatively large initial variance for C if uncertain, e.g. $P_0(3,3)$ large

3) Choose drift rates:
- Smaller $Q_b$ means slower bias adaptation
- Smaller $Q_C$ means slower capacity adaptation

4) Use NIS to tune:
- If average NIS >> 1, increase Q_V (and/or Q_b, Q_C if mismatch is systematic)
- If average NIS << 1, decrease Q_V (or R if R is overestimated)

This EKF removes the need for explicit steady-state detection while continuously estimating the capacity scale from ongoing data.
