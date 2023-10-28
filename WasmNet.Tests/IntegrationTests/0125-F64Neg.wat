;; invoke: neg
;; expect: (f64:-42)

(module
    (func (export "neg") (result f64)
        f64.const 42.0
        f64.neg
    )
)