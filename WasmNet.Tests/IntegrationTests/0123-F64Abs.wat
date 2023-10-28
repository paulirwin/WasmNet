;; invoke: abs
;; expect: (f64:42)

(module
    (func (export "abs") (result f64)
        f64.const -42.0
        f64.abs
    )
)