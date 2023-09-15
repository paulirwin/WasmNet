;; invoke: f64const
;; expect: (f64:1.23)

(module
    (func (export "f64const") (result f64)
        (f64.const 1.23)
    )
)