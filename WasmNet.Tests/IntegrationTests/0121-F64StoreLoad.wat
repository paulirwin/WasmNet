;; invoke: store-load
;; expect: (f64:42)

(module
    (memory 1)
    (func (export "store-load") (result f64)
        i32.const 0 ;; dynamic offset
        f64.const 42.0
        f64.store offset=64
        i32.const 0 ;; dynamic offset
        f64.load offset=64
    )
)